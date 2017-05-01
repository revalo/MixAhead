Imports NAudio
Imports NAudio.Wave
Imports NAudio.Utils
Imports System.IO
Imports MemoryScanner
Imports System.Text
Imports Open.WinKeyboardHook

Public Class Main
    Dim flValue As String
    Public mainOut As AsioOut
    Dim fragReader As FragmentWaveProvider
    Dim flScanner As MemoryScanner.MemoryScanner
    Dim kb As IKeyboardInterceptor
    Dim renderBinName As String = "render"
    Dim folder As String = ""

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Delete and create folder
        Try
            If IO.Directory.Exists(renderBinName) Then
                IO.Directory.Delete(renderBinName, True)
            End If
        Catch
        End Try

        Try
            IO.Directory.CreateDirectory(renderBinName)
        Catch
        End Try

        folder = My.Computer.FileSystem.GetDirectoryInfo(renderBinName).FullName

        ' Initialize our FL memory scanner
        InitializeFLConnection()

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
    Function InitializeFLConnection()
        Dim flInstances As Process() = Diagnostics.Process.GetProcessesByName("FL64")
        If flInstances.Length = 0 Then
            connectionStatus.Text = "Disconnected from FL Studio"
            Return False
        Else
            flScanner = New MemoryScanner.MemoryScanner(flInstances(0))
            connectionStatus.Text = "Connected to FL Studio"
            Return True
        End If
    End Function

    Function IsFlConnected()
        If IsNothing(flScanner) Then
            Return False
        ElseIf flScanner.process.HasExited = True Then
            Return False
        Else
            Return True
        End If
    End Function

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
        Dim uuid As String = Guid.NewGuid().ToString()
        waveToPoll = folder & "\" & uuid & ".wav"

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

    Private Sub Button1_Click(sender As Object, e As EventArgs)
        If flValue.Contains(" to ") Then
            Try
                My.Computer.Clipboard.SetText(SetNewWav())
            Catch ex As Exception
                MsgBox("That didn't work.", MsgBoxStyle.OkOnly, "Naw")
            End Try
        Else
            MsgBox("The timestamp is invalid!", MsgBoxStyle.OkOnly, "Naw")
        End If
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

    Private Sub VolumeSlider1_Load(sender As Object, e As EventArgs)

    End Sub

    Private Sub flConnectionTimer_Tick(sender As Object, e As EventArgs) Handles flConnectionTimer.Tick
        If InitializeFLConnection() = False Then
        Else
            flConnectionTimer.Enabled = False
        End If
    End Sub
End Class

Public Class FragmentWaveProvider
    Implements IWaveProvider

    Public left As List(Of Double)
    Public right As List(Of Double)

    Public wavef As WaveFormat
    Public insertOffset As Long

    Public Sub New()
        Dim reader As New AudioFileReader("ref.wav")
        wavef = reader.WaveFormat
        insertOffset = 0
    End Sub

    ' Main Function that takes in a file and a position in the FL Studio selector, and tries to merge it with the official audio.
    Public Sub MergeAudio(ByVal fileName As String, ByVal flString As String)
        Dim reader As New AudioFileReader(fileName)

        Dim buffer(1) As Single

        Dim startSampleLocation As Long
        Dim endSampleLocation As Long

        Dim tokens As String() = flString.Split(" to ")
        startSampleLocation = ConvertBSTToSamples(tokens(0), reader.WaveFormat) - insertOffset
        endSampleLocation = ConvertBSTToSamples(tokens(2), reader.WaveFormat) - insertOffset

        Dim currentInsertPosition As Integer = startSampleLocation

        If left Is Nothing Then
            left = New List(Of Double)(endSampleLocation - 1)
            right = New List(Of Double)(endSampleLocation - 1)
        ElseIf endSampleLocation >= left.Capacity Then
            left.Capacity = endSampleLocation - 1
            right.Capacity = endSampleLocation - 1
        End If

        reader.Read(buffer, 0, buffer.Length)

        Dim bufferPosition As Long = 0

        If currentInsertPosition < 0 Then
            currentInsertPosition = 0
        End If

        While reader.HasData(2) And currentInsertPosition < endSampleLocation
            reader.Read(buffer, 0, 2)
            If currentInsertPosition < 0 Then
                ' Skip
            End If
            If currentInsertPosition < left.Count Then
                left(currentInsertPosition) = buffer(0)
                right(currentInsertPosition) = buffer(1)
            Else
                left.Add(buffer(0))
                right.Add(buffer(1))
            End If
           

            currentInsertPosition += 1
        End While
    End Sub

    Public Shared Function ConvertBSTToSamples(ByVal bst As String, ByVal wf As WaveFormat) As Long
        Dim b As Integer = bst.Split(":")(0) - 1
        Dim s As Integer = bst.Split(":")(1) - 1
        Dim t As Integer = bst.Split(":")(2)

        s = s + 16 * b
        t = t + 24 * s

        Dim secondsPerTick As Single = 30.0 / 6144.0
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
        Dim startPos As Long = Position
        Dim readPos As Long
        For n As Integer = 0 To count - 1 Step 2
            If Not Position >= left.Count Then
                buffer(n + offset) = CSng(left(Position))
                buffer(n + offset + 1) = CSng(right(Position))
                Position += 1
            Else
                Exit For
            End If
        Next
        readPos = Position

        ' Trigger realloc
        If Position > 1000000 Then
            left.RemoveRange(0, Position)
            right.RemoveRange(0, Position)
            insertOffset += Position
            Position = 0
        End If

        Return readPos - startPos
    End Function

    Public ReadOnly Property Length As Long
        Get
            Return left.Count
        End Get
    End Property

    Public Property Position As Long

    Public ReadOnly Property WaveFormat As WaveFormat Implements IWaveProvider.WaveFormat
        Get
            Return wavef
        End Get
    End Property
End Class