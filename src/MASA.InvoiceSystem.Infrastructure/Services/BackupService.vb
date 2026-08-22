Imports System
Imports System.IO
Imports System.Text
Imports System.Threading.Tasks
Imports Dapper
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Domain.Enums
Imports MASA.InvoiceSystem.Infrastructure.Data
Imports Microsoft.Data.Sqlite
Imports Microsoft.Extensions.Logging

Namespace Services
    Public Class BackupService
        Implements IBackupService

        Private ReadOnly _connFactory As ISqliteConnectionFactory
        Private ReadOnly _auditLogService As IAuditLogService
        Private ReadOnly _logger As ILogger(Of BackupService)

        Public Sub New(connFactory As ISqliteConnectionFactory, auditLogService As IAuditLogService, logger As ILogger(Of BackupService))
            _connFactory = connFactory
            _auditLogService = auditLogService
            _logger = logger
        End Sub

        Public Function ValidateBackupFile(filePath As String) As Boolean Implements IBackupService.ValidateBackupFile
            If String.IsNullOrWhiteSpace(filePath) OrElse Not File.Exists(filePath) Then
                Return False
            End If

            Try
                Dim fileInfo As New FileInfo(filePath)
                If fileInfo.Length < 100 Then
                    Return False
                End If

                Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                    Dim headerBytes(15) As Byte
                    Dim readCount = fs.Read(headerBytes, 0, 16)
                    If readCount < 16 Then Return False

                    Dim headerString = Encoding.ASCII.GetString(headerBytes)
                    Return headerString.StartsWith("SQLite format 3")
                End Using
            Catch ex As Exception
                _logger.LogWarning(ex, "Failed to validate backup file {FilePath}", filePath)
                Return False
            End Try
        End Function

        Public Async Function BackupDatabaseAsync(destinationFilePath As String) As Task(Of String) Implements IBackupService.BackupDatabaseAsync
            Dim sourceDbPath = _connFactory.DatabaseConfig.DatabaseFilePath
            If Not File.Exists(sourceDbPath) Then
                Throw New FileNotFoundException("Source database file was not found.", sourceDbPath)
            End If

            Dim destDir = Path.GetDirectoryName(destinationFilePath)
            If Not String.IsNullOrEmpty(destDir) AndAlso Not Directory.Exists(destDir) Then
                Directory.CreateDirectory(destDir)
            End If

            Using srcConn = Await _connFactory.CreateOpenConnectionAsync()
                Await srcConn.ExecuteAsync("PRAGMA wal_checkpoint(TRUNCATE);")

                Using destConn As New SqliteConnection($"Data Source={destinationFilePath};Pooling=False;")
                    Await destConn.OpenAsync()
                    srcConn.BackupDatabase(destConn)
                    destConn.Close()
                    SqliteConnection.ClearPool(destConn)
                End Using
            End Using
            SqliteConnection.ClearAllPools()

            Await _auditLogService.LogAsync(AuditAction.DatabaseBackedUp, "Database", "0", $"Database backed up to '{destinationFilePath}'.")
            _logger.LogInformation("Database backed up successfully to {Destination}", destinationFilePath)

            Return destinationFilePath
        End Function

        Public Async Function RestoreDatabaseAsync(sourceBackupFilePath As String) As Task(Of Boolean) Implements IBackupService.RestoreDatabaseAsync
            If Not ValidateBackupFile(sourceBackupFilePath) Then
                Throw New InvalidOperationException("The provided backup file is invalid or corrupted.")
            End If

            Dim targetDbPath = _connFactory.DatabaseConfig.DatabaseFilePath
            Dim targetDir = Path.GetDirectoryName(targetDbPath)
            If Not Directory.Exists(targetDir) Then
                Directory.CreateDirectory(targetDir)
            End If

            SqliteConnection.ClearAllPools()

            Dim rollbackTempPath = Path.Combine(Path.GetTempPath(), $"masa_pre_restore_{Guid.NewGuid():N}.bak")
            If File.Exists(targetDbPath) Then
                File.Copy(targetDbPath, rollbackTempPath, overwrite:=True)
            End If

            Try
                Dim walPath = targetDbPath & "-wal"
                Dim shmPath = targetDbPath & "-shm"
                If File.Exists(walPath) Then File.Delete(walPath)
                If File.Exists(shmPath) Then File.Delete(shmPath)

                File.Copy(sourceBackupFilePath, targetDbPath, overwrite:=True)

                Using conn = Await _connFactory.CreateOpenConnectionAsync()
                    Await conn.ExecuteAsync("PRAGMA integrity_check;")
                End Using

                If File.Exists(rollbackTempPath) Then File.Delete(rollbackTempPath)

                Await _auditLogService.LogAsync(AuditAction.DatabaseRestored, "Database", "0", $"Database restored from '{sourceBackupFilePath}'.")
                _logger.LogInformation("Database successfully restored from {BackupPath}", sourceBackupFilePath)
                Return True
            Catch ex As Exception
                _logger.LogError(ex, "Database restore failed. Initiating automatic rollback.")
                If File.Exists(rollbackTempPath) Then
                    File.Copy(rollbackTempPath, targetDbPath, overwrite:=True)
                    File.Delete(rollbackTempPath)
                End If
                Throw New InvalidOperationException($"Restore failed and previous state was rolled back: {ex.Message}", ex)
            End Try
        End Function
    End Class
End Namespace
