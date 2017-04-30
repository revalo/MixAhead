﻿Public Class Selector_Display

    Private Sub Label1_Click(sender As Object, e As EventArgs) Handles Label1.Click

    End Sub

    Private Sub Selector_Display_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.Location = New Point(0, 0)
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If Main.flLiveStatus = "" Then
            Me.Opacity = 0
        Else
            Me.Opacity = 1
        End If
        Label1.Text = Main.flLiveStatus
    End Sub
    ' Prevent from showing in Alt-Tab
    Protected Overrides ReadOnly Property CreateParams() As CreateParams
        Get
            ' Turn on WS_EX_TOOLWINDOW style bit
            Dim cp As CreateParams = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or &H80
            Return cp
        End Get
    End Property
End Class