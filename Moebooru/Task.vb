﻿Imports System.IO.Compression
Imports Microsoft.VisualBasic.ApplicationServices
Imports Microsoft.VisualBasic.Language
Imports Moebooru.Models

Public Module Task

    ''' <summary>
    ''' 返回缺失的post编号
    ''' </summary>
    ''' <param name="directory"></param>
    ''' <returns></returns>
    Public Function CheckPoolIntegrity(directory As String) As Integer()
        Dim index As Pool = $"{directory}/index.xml".LoadXml(Of Pool)
        Dim missing As New List(Of Integer)

        For Each POST In index.posts
            Dim file$ = $"{directory}/{POST.id}.{POST.file_url.ExtensionSuffix}"
            Dim test = file.FileLength > 0

            If Not test = True Then
                missing += POST.id
            End If
        Next

        Return missing
    End Function

    Public Function DownloadPool(id$, EXPORT$) As IEnumerable(Of (file$, success As Boolean))
        Dim result = api.DownloadPool(id, EXPORT)
        Dim pool As Pool = $"{EXPORT}/index.Xml".LoadXml(Of Pool)
        Dim zip$ = $"{EXPORT.ParentPath}/{pool.name.NormalizePathString(False)}.zip"

        Call GZip.DirectoryArchive(
            EXPORT, zip,
            ArchiveAction.Replace,
            Overwrite.Always,
            CompressionLevel.Fastest,
            flatDirectory:=True
        )
        Return result
    End Function
End Module
