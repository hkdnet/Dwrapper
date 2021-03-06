﻿Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports DapperWrapper
Imports System.Data.Common
Imports System.Data.SQLite

<TestClass()>
Public Class DwrapperTest
#Region "test helper class"
    Public Class SQLiteDwrapperForTest
        Inherits SQLiteDwrapper
        Sub New()
            MyBase.New("DataSource=test.db")
        End Sub
        Function IsClose() As Boolean
            Return _conn.State = ConnectionState.Closed
        End Function
    End Class
    Public Class Count
        Public Property Count As Integer
    End Class
    Public Class Member
        Public Property Id As Integer
        Public Property Name As String
    End Class
#End Region
    Private _members As Member() = New Member() {New Member() With {.Name = "nico yazawa"},
                                                 New Member() With {.Name = "maki nishikino"},
                                                 New Member() With {.Name = "nozomi tojo"}}

#Region "beforetest"
    <TestInitialize>
    Public Sub BeforeTest()
        Dim dropSql = "DROP TABLE IF EXISTS member"
        Dim createSql = "CREATE TABLE member(id INTEGER  PRIMARY KEY AUTOINCREMENT, name TEXT)"
        Dim insertSql = "INSERT INTO member (name) VALUES ('{0}')"
        Dim dapperWrapper = New SQLiteDwrapperForTest()
        Using dapperWrapper.Open
            Using tran = dapperWrapper.BeginTransaction
                dapperWrapper.Execute(dropSql)
                dapperWrapper.Execute(createSql)
                For Each member In _members
                    dapperWrapper.Execute(String.Format(insertSql, member.Name))
                Next
                tran.Commit()
            End Using
        End Using
    End Sub
#End Region
#Region "接続"
    <TestMethod()>
    <ExpectedException(GetType(ObjectDisposedException))>
    Public Sub Using句によって接続が閉じられること()
        Dim dapperWrapper = New SQLiteDwrapperForTest()
        Using dapperWrapper.Open
            Using tran = dapperWrapper.BeginTransaction
                Dim sql = "DELETE FROM member WHERE name = '{0}'"
                dapperWrapper.Execute(String.Format(sql, _members.First.Name))
                tran.Commit()
            End Using
            Assert.IsFalse(dapperWrapper.IsClose)
        End Using
        dapperWrapper.IsClose()
    End Sub
    <TestMethod>
    Public Sub OpenしないときにAutoOpenできること()
        Dim dapperWrapper = New SQLiteDwrapperForTest()
        Dim sql = "SELECT COUNT(*) AS count FROM member"
        Dim c = dapperWrapper.QueryTop1(Of Count)(sql)
        Assert.AreEqual(3, c.Count)
        Assert.IsTrue(dapperWrapper.IsClose())
    End Sub
    <TestMethod()>
    Public Sub Transactionを張った時CommitしないとRollbackされること()
        Dim dwrapper = New SQLiteDwrapperForTest()
        Dim selectSql = "SELECT COUNT(*) AS count FROM member"
        Using dwrapper.Open
            Dim c = dwrapper.QueryTop1(Of Count)(selectSql)
            Assert.AreEqual(_members.Count, c.Count)
            Using tran = dwrapper.BeginTransaction
                Dim sql = "DELETE FROM member WHERE name = '{0}'"
                dwrapper.Execute(String.Format(sql, _members.First.Name))
                c = dwrapper.QueryTop1(Of Count)(selectSql)
                Assert.AreEqual(_members.Count - 1, c.Count)
            End Using
        End Using
        Dim dwrapper2 = New SQLiteDwrapperForTest
        Assert.AreEqual(_members.Count, dwrapper2.QueryTop1(Of Count)(selectSql).Count)
    End Sub
    <TestMethod()>
    Public Sub Transactionを張った時Commitすると確定されること()
        Dim selectSql = "SELECT COUNT(*) AS count FROM member"
        Dim dwrapper = New SQLiteDwrapperForTest()
        Using dwrapper.Open
            Dim c = dwrapper.QueryTop1(Of Count)(selectSql)
            Assert.AreEqual(_members.Count, c.Count)
            Using tran = dwrapper.BeginTransaction
                Dim sql = "DELETE FROM member WHERE name = '{0}'"
                dwrapper.Execute(String.Format(sql, _members.First.Name))
                c = dwrapper.QueryTop1(Of Count)(selectSql)
                Assert.AreEqual(_members.Count - 1, c.Count)
                dwrapper.Commit()
            End Using
        End Using
        Dim dwrapper2 = New SQLiteDwrapperForTest
        Assert.AreEqual(_members.Count - 1, dwrapper2.QueryTop1(Of Count)(selectSql).Count)
    End Sub
#End Region

#Region "イベント関連"
    <TestMethod()>
    Public Sub BeforeSQLイベントにイベントを追加できること()
        Dim dapperWrapper = New SQLiteDwrapperForTest()
        Dim beforeSqlCount = 0
        AddHandler dapperWrapper.BeforeSql, Sub(_1, _2)
                                                beforeSqlCount += 1
                                            End Sub
        Using dapperWrapper.Open
            Assert.AreEqual(0, beforeSqlCount)
            Dim selectSql = "SELECT COUNT(*) AS count FROM member"
            Dim c = dapperWrapper.Query(Of Count)(selectSql)
            Assert.AreEqual(1, beforeSqlCount)
        End Using
    End Sub
    <TestMethod()>
    Public Sub AfterSQLイベントにイベントを追加できること()
        Dim dapperWrapper = New SQLiteDwrapperForTest()
        Dim afterSqlCount = 0
        AddHandler dapperWrapper.AfterSql, Sub(_1, _2)
                                               afterSqlCount += 1
                                           End Sub
        Using dapperWrapper.Open
            Assert.AreEqual(0, afterSqlCount)
            Dim deleteSql = "DELETE FROM member WHERE name = '{0}'"
            Using dapperWrapper.BeginTransaction
                dapperWrapper.Execute(String.Format(deleteSql, _members.Last.Name))
                Assert.AreEqual(1, afterSqlCount)
                Dim selectSql = "SELECT COUNT(*) AS count FROM member"
                Dim c = dapperWrapper.Query(Of Member)(selectSql)
                Assert.AreEqual(2, afterSqlCount)
                dapperWrapper.Commit()
            End Using
        End Using
    End Sub
    <TestMethod()>
    Public Sub AfterQueryイベントにイベントを追加できること()
        Dim dapperWrapper = New SQLiteDwrapperForTest()
        Dim afterQueryCount = 0
        AddHandler dapperWrapper.AfterQuery, Sub(_1, _2, _3, _4)
                                                 afterQueryCount += 1
                                             End Sub
        Using dapperWrapper.Open
            Assert.AreEqual(0, afterQueryCount)
            Dim selectSql = "SELECT COUNT(*) AS count FROM member"
            Dim c = dapperWrapper.Query(Of Member)(selectSql)
            Assert.AreEqual(1, afterQueryCount)
        End Using
    End Sub
    <TestMethod()>
    Public Sub ExecuteのあとにAfterQueryイベントが発火されないこと()
        Dim dapperWrapper = New SQLiteDwrapperForTest()
        Dim afterQueryCount = 0
        AddHandler dapperWrapper.AfterQuery, Sub(_1, _2, _3, _4)
                                                 afterQueryCount += 1
                                             End Sub
        Using dapperWrapper.Open
            Assert.AreEqual(0, afterQueryCount)
            Using tran = dapperWrapper.BeginTransaction
                Dim sql = "DELETE FROM member WHERE name = '{0}'"
                dapperWrapper.Execute(String.Format(sql, _members.First.Name))
                dapperWrapper.Commit()
            End Using
            Assert.AreEqual(0, afterQueryCount)
        End Using
    End Sub
    <TestMethod()>
    Public Sub AfterExecuteイベントにイベントを追加できること()
        Dim dapperWrapper = New SQLiteDwrapperForTest()
        Dim afterExecuteCount = 0
        AddHandler dapperWrapper.AfterExecute, Sub(_1, _2, _3)
                                                   afterExecuteCount += 1
                                               End Sub
        Using dapperWrapper.Open
            Assert.AreEqual(0, afterExecuteCount)
            Using tran = dapperWrapper.BeginTransaction
                Dim sql = "DELETE FROM member WHERE name = '{0}'"
                dapperWrapper.Execute(String.Format(sql, _members.First.Name))
                dapperWrapper.Commit()
            End Using
            Assert.AreEqual(1, afterExecuteCount)
        End Using
    End Sub
    <TestMethod()>
    Public Sub QueryのあとにAfterExecuteイベントが発火されないこと()
        Dim dapperWrapper = New SQLiteDwrapperForTest()
        Dim afterExecuteCount = 0
        AddHandler dapperWrapper.AfterExecute, Sub(_1, _2, _3)
                                                   afterExecuteCount += 1
                                               End Sub
        Using dapperWrapper.Open
            Assert.AreEqual(0, afterExecuteCount)
            Dim selectSql = "SELECT COUNT(*) AS count FROM member"
            Dim c = dapperWrapper.Query(Of Member)(selectSql)
            Assert.AreEqual(0, afterExecuteCount)
        End Using
    End Sub
#End Region
End Class