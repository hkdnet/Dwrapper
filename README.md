Dwrapper
====

Overview

A simple wrapper for [Dapper](https://github.com/StackExchange/dapper-dot-net).

## Description

### Features

* auto open
* default connection string
* provide events that is fired when before/after data access

## Requirement

See .nuget for using packages.  
SQLite is needed only when running tests.  
It's not necessary if you don't use SQLite :+1:

## Usage

Here is an excample.

note:  
Dwrapper doesn't support SQLite parameter mapping, sorry.  
If you use SQLConnection instead, you can write this more simple.

```vb
Dim dwrapper = New SQLiteDwrapper()
Using dwrapper.Open
    Assert.AreEqual(0, afterSqlCount)
    Dim deleteSql = "DELETE FROM member WHERE name = '{0}'"
    Using dwrapper.BeginTransaction
        dwrapper.Execute(String.Format(deleteSql, _members.Last.Name))
        Assert.AreEqual(1, afterSqlCount)
        dwrapper.Commit()
    End Using
    Dim selectSql = "SELECT COUNT(*) AS count FROM member"
    Dim c = dwrapper.Query(Of Member)(selectSql)
    Assert.AreEqual(2, afterSqlCount)
End Using
```

## Install

copy and paste only, sorry.

## Contribution

Anything is welcome!

## Licence

[MIT](https://github.com/hkdnet/Dwrapper/blob/master/LICENSE)

## Author

[hkdnet](https://github.com/hkdnet)