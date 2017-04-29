Imports NAudio
Imports NAudio.Wave
Imports NAudio.Utils
Imports System.IO
Imports MemoryScanner
Imports System.Text
Imports Open.WinKeyboardHook

Public Class Form1
    Dim flValue As String
    Public mainOut As AsioOut
    Dim fragReader As FragmentWaveProvider
    Dim flScanner As MemoryScanner.MemoryScanner
    Dim kb As IKeyboardInterceptor

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Initialize our FL memory scanner
        flScanner = New MemoryScanner.MemoryScanner(System.Diagnostics.Process.GetProcessesByName("FL64")(0))

        ' Initialize poller thread
        Dim t As New Threading.Thread(AddressOf FlPollerThread)
        t.Start()

        ' Initialize our wave reading engine
        fragReader = New FragmentWaveProvider()

        ' Fill items in the audio setup
        MonitorOutput.Items.Clear()
        MonitorOutput.Items.AddRange(AsioOut.GetDriverNames())

        If MonitorOutput.Items.Contains(My.Settings.monitorName) Then
            MonitorOutput.SelectedItem = My.Settings.monitorName
            InitializeAudio()
        End If

        ' Initialize keyboard hook
        kb = New KeyboardInterceptor()
        AddHandler kb.KeyDown, AddressOf GlobalKeyDown
        AddHandler kb.KeyUp, AddressOf GlobalKeyUp

        kb.StartCapturing()

        ' Start the HUD
        Selector_Display.Show()
    End Sub
    Public flLiveStatus As String
    Sub FlPollerThread()
        Do
            Try
                Dim res() As String = flScanner.ScanRegex("[0-9]*:[0-9]*:[0-9]* to [0-9]*:[0-9]*:[0-9][0-9]")
                If res.Length > 0 Then
                    flValue = res(0)
                    flLiveStatus = res(0)
                Else
                    flLiveStatus = ""
                End If
            Catch ex As Exception
                ex = ex
            End Try
            Threading.Thread.Sleep(50)
        Loop
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles MonitorOutput.SelectedIndexChanged
        InitializeAudio()
        My.Settings.monitorName = MonitorOutput.SelectedItem.ToString()
        My.Settings.Save()
    End Sub

    Public Sub InitializeAudio()
        Try
            mainOut = New AsioOut(MonitorOutput.SelectedItem.ToString())
        Catch ex As Exception

        End Try
    End Sub

    Dim waveToPoll As String
    Dim wavePoller As Threading.Thread
    ' TO-DO: Jank
    Public Function SetNewWav() As String
        Dim folder As String = "C:\Users\shrey\Documents\Visual Studio 2012\Projects\MixAhead\MixAhead\bin\Debug\render\"
        Dim uuid As String = Guid.NewGuid().ToString()
        waveToPoll = folder & uuid & ".wav"

        If Not wavePoller Is Nothing Then
            Try
                wavePoller.Suspend()
            Catch ex As Exception

            End Try
        End If

        wavePoller = New Threading.Thread(AddressOf wavePollingThread)
        wavePoller.Start()

        Return waveToPoll
    End Function

    Sub wavePollingThread()
        Do
            If IO.File.Exists(waveToPoll) And Not IsFileLocked(My.Computer.FileSystem.GetFileInfo(waveToPoll)) Then
                fragReader.MergeAudio(waveToPoll, flValue)
                If mainOut.PlaybackState = PlaybackState.Stopped Then
                    mainOut.Init(fragReader)
                    mainOut.Play()
                End If

                Exit Do
            End If
            Threading.Thread.Sleep(1000)
        Loop
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        My.Computer.Clipboard.SetText(SetNewWav())
    End Sub

    Protected Overridable Function IsFileLocked(file As FileInfo) As Boolean
        Dim stream As FileStream = Nothing

        Try
            stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None)
        Catch generatedExceptionName As IOException
            'the file is unavailable because it is:
            'still being written to
            'or being processed by another thread
            'or does not exist (has already been processed)
            Return True
        Finally
            If stream IsNot Nothing Then
                stream.Close()
            End If
        End Try

        'file is not locked
        Return False
    End Function

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        mainOut.ShowControlPanel()
    End Sub

    Dim ctrlDown As Boolean = False
    Private Sub GlobalKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.LControlKey Then
            ctrlDown = True
        End If
        If e.KeyCode = Keys.R And ctrlDown Then
            Button1_Click(Nothing, Nothing)
        End If
    End Sub

    Private Sub GlobalKeyUp(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.LControlKey Then
            ctrlDown = False
        End If
    End Sub

End Class

Public Class FragmentWaveProvider
    Implements IWaveProvider

    Public left As Double()
    Public right As Double()

    Public wavef As WaveFormat

    Public Sub New()
        Dim reader As New AudioFileReader("C:\Users\shrey\Documents\Visual Studio 2012\Projects\MixAhead\MixAhead\bin\Debug\render\ref.wav")
        wavef = reader.WaveFormat
    End Sub

    ' Main Function that takes in a file and a position in the FL Studio selector, and tries to merge it with the official audio.
    Public Sub MergeAudio(ByVal fileName As String, ByVal flString As String)
        Dim reader As New AudioFileReader(fileName)

        Dim buffer(1) As Single

        Dim startSampleLocation As Long
        Dim endSampleLocation As Long

        Dim tokens As String() = flString.Split(" to ")
        startSampleLocation = ConvertBSTToSamples(tokens(0), reader.WaveFormat)
        endSampleLocation = ConvertBSTToSamples(tokens(2), reader.WaveFormat)

        Dim currentInsertPosition As Integer = startSampleLocation

        If left Is Nothing Then
            ReDim left(endSampleLocation - 1)
            ReDim right(endSampleLocation - 1)
        ElseIf endSampleLocation >= left.Length Then
            ReDim Preserve left(endSampleLocation - 1)
            ReDim Preserve right(endSampleLocation - 1)
        End If

        reader.Read(buffer, 0, buffer.Length)

        Dim bufferPosition As Long = 0

        While reader.HasData(2) And currentInsertPosition < endSampleLocation
            reader.Read(buffer, 0, 2)
            left(currentInsertPosition) = buffer(0)
            right(currentInsertPosition) = buffer(1)

            currentInsertPosition += 1
        End While
    End Sub

    Public Shared Function ConvertBSTToSamples(ByVal bst As String, ByVal wf As WaveFormat) As Long
        Dim b As Integer = bst.Split(":")(0) - 1
        Dim s As Integer = bst.Split(":")(1) - 1
        Dim t As Integer = bst.Split(":")(2)

        s = s + 16 * b
        t = t + 24 * s

        Dim secondsPerTick As Single = 0.0045833333
        Dim totalSeconds As Double = secondsPerTick * t

        Dim sample As Long = totalSeconds * wf.SampleRate
        Return sample
    End Function

    Public Function Read(buffer As Byte(), offset As Integer, count As Integer) As Integer Implements IWaveProvider.Read
        Dim waveBuffer As New WaveBuffer(buffer)
        Dim samplesRequired As Integer = count / 4
        Dim samplesRead As Integer = Read(waveBuffer.FloatBuffer, offset / 4, samplesRequired)
        Return samplesRead * 4
    End Function

    Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer
        For n As Integer = 0 To count - 1 Step 2
            buffer(n + offset) = CSng(left(Position))
            buffer(n + offset + 1) = CSng(right(Position))
            Position += 1
        Next
        Return count
    End Function

    Public ReadOnly Property Length As Long
        Get
            Return left.Length
        End Get
    End Property

    Public Property Position As Long

    Public ReadOnly Property WaveFormat As WaveFormat Implements IWaveProvider.WaveFormat
        Get
            Return wavef
        End Get
    End Property
End Class