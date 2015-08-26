Option Strict On

Imports Dapper
Imports System.Data.SqlClient
Imports System.Data.Common

Public MustInherit Class DwrapperBase
    Protected _connStr As String
    Protected _conn As DbConnection
    Protected _tran As DbTransaction
    Protected _sql As String
    Protected _data As Object
    Private _isAutoOpen As Boolean
    Public Timeout As Integer?
    Public Event BeforeSql(sql As String, data As Object)
    Public Event AfterSql(sql As String, data As Object)
    Public Event AfterExecute(sql As String, data As Object, count As Integer)
    Public Event AfterQuery(sql As String, data As Object, res As IEnumerable, type As Type)

    Sub New(connStr As String)
        _connStr = connStr
        SetupAutoOpen()
        SetupAutoClose()
    End Sub

    Protected Sub SetupAutoOpen()
        AddHandler BeforeSql, Sub(sql, data)
                                  If _conn Is Nothing Then
                                      _isAutoOpen = True
                                      _conn = Open()
                                  Else
                                      _isAutoOpen = False
                                  End If
                              End Sub
    End Sub
    Protected Sub SetupAutoClose()
        AddHandler AfterSql, Sub(sql, data)
                                 If _isAutoOpen Then
                                     _conn.Close()
                                 End If
                             End Sub
    End Sub

    MustOverride Function Open() As DbConnection

    Function BeginTransaction() As DbTransaction
        _tran = _conn.BeginTransaction
        Return _tran
    End Function

    Public Sub Commit()
        If _tran IsNot Nothing Then
            _tran.Commit()
        End If
    End Sub
    Public Function Execute(sql As String,
                            Optional data As Object = Nothing) As Integer
        _sql = sql
        _data = data
        RaiseEvent BeforeSql(sql, data)
        Dim ret As Integer
        If _tran Is Nothing Then
            ret = _conn.Execute(_sql, param:=_data, commandTimeout:=Timeout)
        Else
            ret = _conn.Execute(_sql, param:=_data, commandTimeout:=Timeout, transaction:=_tran)
        End If
        RaiseEvent AfterSql(sql, data)
        RaiseEvent AfterExecute(sql, data, ret)
        Return ret
    End Function
    Public Function Query(Of T As Class)(sql As String,
                                         Optional data As Object = Nothing) As IEnumerable(Of T)
        _sql = sql
        _data = data
        RaiseEvent BeforeSql(sql, data)
        Dim ret = _conn.Query(Of T)(_sql, param:=_data, commandTimeout:=Timeout)
        RaiseEvent AfterSql(sql, data)
        RaiseEvent AfterQuery(sql, data, ret, GetType(T))
        Return ret
    End Function
    Public Function QueryTop1(Of T As Class)(sql As String,
                                             Optional data As Object = Nothing) As T
        Return Me.Query(Of T)(sql, data:=data).FirstOrDefault
    End Function

End Class

