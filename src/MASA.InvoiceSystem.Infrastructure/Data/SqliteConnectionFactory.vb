Imports System.Data
Imports System.Data.Common
Imports System.Threading.Tasks
Imports Microsoft.Data.Sqlite

Namespace Data
    Public Interface ISqliteConnectionFactory
        Function CreateConnection() As SqliteConnection
        Function CreateOpenConnectionAsync() As Task(Of SqliteConnection)
        ReadOnly Property DatabaseConfig As DatabaseConfig
    End Interface

    Public Class SqliteConnectionFactory
        Implements ISqliteConnectionFactory

        Private ReadOnly _config As DatabaseConfig

        Public Sub New(config As DatabaseConfig)
            _config = config
        End Sub

        Public ReadOnly Property DatabaseConfig As DatabaseConfig Implements ISqliteConnectionFactory.DatabaseConfig
            Get
                Return _config
            End Get
        End Property

        Public Function CreateConnection() As SqliteConnection Implements ISqliteConnectionFactory.CreateConnection
            Dim conn As New SqliteConnection(_config.ConnectionString)
            Return conn
        End Function

        Public Async Function CreateOpenConnectionAsync() As Task(Of SqliteConnection) Implements ISqliteConnectionFactory.CreateOpenConnectionAsync
            Dim conn = CreateConnection()
            Await conn.OpenAsync()

            ' Explicitly enforce foreign key constraints
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL;"
                Await cmd.ExecuteNonQueryAsync()
            End Using

            Return conn
        End Function
    End Class
End Namespace
