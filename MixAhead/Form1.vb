Imports NAudio
Imports NAudio.Wave

Public Class Form1
    Dim flValue As String
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Initialize poller thread
        Dim t As New Threading.Thread(AddressOf FlPollerThread)
        t.Start()

        ' Fill items in the audio setup
        MainOutput.Items.Clear()
        MainOutput.Items.AddRange(AsioOut.GetDriverNames())

        MonitorOutput.Items.Clear()
        MonitorOutput.Items.AddRange(AsioOut.GetDriverNames())

        If MainOutput.Items.Contains(My.Settings.mainName) Then
            MainOutput.SelectedItem = My.Settings.mainName
        End If

        If MonitorOutput.Items.Contains(My.Settings.monitorName) Then
            MonitorOutput.SelectedItem = My.Settings.monitorName
        End If
    End Sub

    Sub FlPollerThread()
        Do
            Dim p As Process = New Process()
            p.StartInfo.FileName = "Capture2Text_CLI.exe"
            p.StartInfo.Arguments = "-s ""10 50 200 76"""
            p.StartInfo.UseShellExecute = False
            p.StartInfo.RedirectStandardOutput = True
            p.StartInfo.CreateNoWindow = True
            p.Start()
            p.WaitForExit()
            Me.Text = p.StandardOutput.ReadToEnd()
            Threading.Thread.Sleep(10)
        Loop
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedIndexChanged

    End Sub
End Class

Public Class FragmentWaveProvider
    Inherits WaveStream

    Public Sub New()

    End Sub

    Public Function GetDuration() As TimeSpan
        If tracks.Count = 0 Then
            Return New TimeSpan(0)
        End If
        Dim lastTrack As Track = tracks.OrderBy(Function(t) t.startTime + t.duration).Last()
        duration = lastTrack.startTime + lastTrack.duration
        Return duration
    End Function

    Public Overrides Function Read(buffer As Byte(), offset As Integer, count As Integer) As Integer
        Dim waveBuffer As New WaveBuffer(buffer)
        Dim samplesRequired As Integer = count / 4
        Dim samplesRead As Integer = Read(waveBuffer.FloatBuffer, offset / 4, samplesRequired)
        Return samplesRead * 4
    End Function
    'Private sample As Integer = 0

    Public Function MixAllLeft(ByVal sample As Integer) As Single
        Dim startSample As Integer = 0
        Dim total As Single = 0
        Dim access As Integer
        For Each t In tracks
            startSample = t.startTime.TotalSeconds * wavef.SampleRate
            access = sample - startSample
            If access >= 0 And access < t.left.Length Then
                total += t.left(access)
            End If
        Next

        If total > 1 Then
            Return 1
        ElseIf total < -1 Then
            Return -1
        End If
        Return total
    End Function

    Public Function MixAllRight(ByVal sample As Integer) As Single
        Dim startSample As Integer = 0
        Dim total As Single = 0
        Dim access As Integer
        For Each t In tracks
            startSample = t.startTime.TotalSeconds * wavef.SampleRate
            access = sample - startSample
            If access >= 0 And access < t.right.Length Then
                total += t.right(access)
            End If
        Next

        If total > 1 Then
            Return 1
        ElseIf total < -1 Then
            Return -1
        End If
        Return total
    End Function

    Public Overloads Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer
        For n As Integer = 0 To count - 1 Step 2
            buffer(n + offset) = CSng(MixAllLeft(Position))
            buffer(n + offset + 1) = CSng(MixAllRight(Position))
            Position += 1
        Next
        Return count
    End Function

    Public Overrides ReadOnly Property Length As Long
        Get
            Return GetDuration().TotalSeconds * WaveFormat.SampleRate
        End Get
    End Property

    Public Overrides Property Position As Long

    Public Overrides ReadOnly Property WaveFormat As WaveFormat
        Get
            Return wavef
        End Get
    End Property
End Class