Imports System

Namespace Exceptions
    Public Class ValidationException
        Inherits Exception

        Public ReadOnly Property ValidationErrors As IReadOnlyList(Of String)

        Public Sub New(message As String)
            MyBase.New(message)
            ValidationErrors = New List(Of String) From {message}
        End Sub

        Public Sub New(errors As IEnumerable(Of String))
            MyBase.New(String.Join("; ", If(errors, New String() {})))
            ValidationErrors = New List(Of String)(If(errors, New String() {}))
        End Sub
    End Class

    Public Class NotFoundException
        Inherits Exception

        Public Sub New(entityName As String, key As Object)
            MyBase.New($"Entity '{entityName}' with key '{key}' was not found.")
        End Sub

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub
    End Class

    Public Class BusinessRuleException
        Inherits Exception

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub
    End Class
End Namespace
