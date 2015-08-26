Imports System.Data.SQLite

Public Class SQLiteDwrapper
    Inherits DwrapperBase
    Sub New(connStr As String)
        MyBase.New(connStr)
    End Sub
    Public Overrides Function Open() As Common.DbConnection
        _conn = New SQLiteConnection(_connStr)
        _conn.Open()
        Return _conn
    End Function
End Class
