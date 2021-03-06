﻿Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.VisualBasic.ApplicationServices.Terminal.ProgressBar
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Net.HTTP
Imports Moebooru.Models

<HideModuleName> Public Module api

    Const base$ = "https://yande.re"

    ReadOnly apis As Dictionary(Of String, String)
    ReadOnly proxy As request.Proxy
    ReadOnly httpGetText As Func(Of String, String)

    Private Function getURL(<CallerMemberName> Optional key$ = Nothing) As String
        Return $"{api.base}/{apis(key)}"
    End Function

    Sub New()
        apis = GetType(api) _
            .GetMethods(BindingFlags.Public Or BindingFlags.Static) _
            .Where(Function(m)
                       Return Not m.GetCustomAttribute(Of ExportAPIAttribute) Is Nothing
                   End Function) _
            .ToDictionary(Function(m) m.Name,
                          Function(m)
                              Return m.GetCustomAttribute(Of ExportAPIAttribute).Name
                          End Function)

        If App.CommandLine.ContainsParameter("--proxy") Then
            proxy = New request.Proxy(App.CommandLine <= "--proxy")
        End If

        If proxy Is Nothing Then
            httpGetText = AddressOf HttpGet.GET
        Else
            httpGetText = AddressOf proxy.GetText
        End If
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="limit%">How many posts you want to retrieve. There is a hard limit of 100 posts per request.</param>
    ''' <param name="page%">The page number.</param>
    ''' <param name="tags">
    ''' The tags to search for. Any tag combination that works on the web site will work here. This includes all the meta-tags.
    ''' </param>
    ''' <returns></returns>
    <ExportAPI("post.xml")>
    Public Function Posts(Optional limit% = -1, Optional page% = -1, Optional tags As IEnumerable(Of String) = Nothing) As Posts
        Dim url$ = getURL()

        With tags.SafeQuery.ToArray
            If .Length > 0 Then
                url = $"{url}?tags={ .JoinBy("+")}"
            End If
        End With

        If page > 0 Then
            url = $"{url}&page={page}"
        End If

        Dim out = httpGetText(url).LoadFromXml(Of Posts)
        Return out
    End Function

    <ExportAPI("pool/show.xml")>
    Public Function PoolShow(id As String) As Pool
        Dim url$ = getURL() & $"?id={id}"
        Dim out = httpGetText(url).LoadFromXml(Of Pool)
        Return out
    End Function

    ''' <summary>
    ''' 这个函数会自动跳过已经存在的文件的下载操作
    ''' </summary>
    ''' <param name="id$"></param>
    ''' <param name="EXPORT$"></param>
    ''' <returns></returns>
    Public Function DownloadPool(id$, EXPORT$) As IEnumerable(Of (file$, success As Boolean))
        Dim pool As Pool = api.PoolShow(id)
        Dim result = pool.posts.DownloadPostList(EXPORT).ToArray

        Call pool.GetXml.SaveTo($"{EXPORT}/index.xml")

        Return result
    End Function

    <Extension>
    Public Iterator Function DownloadPostList(posts As post(), EXPORT$) As IEnumerable(Of (file$, success As Boolean))
        Using progressBar As New ProgressBar("Download pool images...", 1, CLS:=True)
            Dim task As New ProgressProvider(progressBar, total:=posts.Length)
            Dim msg$

            For Each post As post In posts
                Dim url$ = post.file_url
                Dim save$ = $"{EXPORT}/{post.id}.{url.ExtensionSuffix}"

                msg = $" ==> {url.BaseName(allowEmpty:=True)} [ETA={task.ETA(progressBar.ElapsedMilliseconds).FormatTime}]"

                Call progressBar.SetProgress(task.StepProgress, msg)

                If url.StringEmpty Then
                    Continue For
                End If

                If Not save.FileExists OrElse save.LoadImage(throwEx:=False) Is Nothing Then
                    Yield (url, wget.Download(url, save))

                    Call Thread.Sleep(10 * 1000)
                Else
                    Yield (url, True)
                End If
            Next
        End Using
    End Function
End Module
