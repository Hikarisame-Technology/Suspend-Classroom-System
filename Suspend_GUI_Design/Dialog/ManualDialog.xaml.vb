Public Class ManualDialog
    Dim ProcessVersion = Application.ResourceAssembly.GetName().Version.ToString()
    Private Sub ContentDialog_Loaded(sender As Object, e As RoutedEventArgs)
        'Version.Text = ProcessVersion
    End Sub
End Class
