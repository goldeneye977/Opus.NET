namespace OpusDemo
{
    using System.Windows.Forms;
    using System;
    using NAudio.Wave;
    using FragLabs.Audio.Codecs;
    using FragLabs.Audio.Codecs.Opus;

    public partial class Form1 : Form
    {
        private WaveIn _waveIn;
        private WaveOut _waveOut;
        private BufferedWaveProvider _playBuffer;
        private OpusEncoder _encoder;
        private OpusDecoder _decoder;
        private int _segmentFrames;
        private int _bytesPerSegment;
        private ulong _bytesSent;
        private DateTime _startTime;
        private Timer _timer;
        private byte[] _notEncodedBuffer = Array.Empty<byte>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                comboBox1.Items.Add(WaveIn.GetCapabilities(i).ProductName);
            }

            if (WaveIn.DeviceCount > 0)
            {
                comboBox1.SelectedIndex = 0;
            }

            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                comboBox2.Items.Add(WaveOut.GetCapabilities(i).ProductName);
            }

            if (WaveOut.DeviceCount > 0)
            {
                comboBox2.SelectedIndex = 0;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button2.Enabled = true;
            button1.Enabled = false;
            StartEncoding();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = true;
            button2.Enabled = false;
            StopEncoding();
        }

        private void StartEncoding()
        {
            _startTime = DateTime.Now;
            _bytesSent = 0;
            _segmentFrames = 960;
            _encoder = OpusEncoder.Create(inputSamplingRate: 48000, inputChannels:1, CodingMode.Voip);
            _encoder.Bitrate = 8192;
            _decoder = OpusDecoder.Create(48000, 1);
            _bytesPerSegment = _encoder.FrameByteCount(_segmentFrames);

            _waveIn = new WaveIn(WaveCallbackInfo.FunctionCallback());
            _waveIn.BufferMilliseconds = 50;
            _waveIn.DeviceNumber = comboBox1.SelectedIndex;
            _waveIn.DataAvailable += _waveIn_DataAvailable;
            _waveIn.WaveFormat = new WaveFormat(48000, 16, 1);

            _playBuffer = new BufferedWaveProvider(new WaveFormat(48000, 16, 1));

            _waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback());
            _waveOut.DeviceNumber = comboBox2.SelectedIndex;
            _waveOut.Init(_playBuffer);

            _waveOut.Play();
            _waveIn.StartRecording();

            if (_timer == null)
            {
                _timer = new Timer();
                _timer.Interval = 1000;
                _timer.Tick += _timer_Tick;
            }

            _timer.Start();
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            var timeDiff = DateTime.Now - _startTime;
            var bytesPerSecond = _bytesSent / timeDiff.TotalSeconds;
            Console.WriteLine("{0} Bps", bytesPerSecond);
        }

        private void _waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] soundBuffer = new byte[e.BytesRecorded + _notEncodedBuffer.Length];
            for (int i = 0; i < _notEncodedBuffer.Length; i++)
                soundBuffer[i] = _notEncodedBuffer[i];
            for (int i = 0; i < e.BytesRecorded; i++)
                soundBuffer[i + _notEncodedBuffer.Length] = e.Buffer[i];

            int byteCap = _bytesPerSegment;
            int segmentCount = (int)Math.Floor((decimal)soundBuffer.Length / byteCap);
            int segmentsEnd = segmentCount * byteCap;
            int notEncodedCount = soundBuffer.Length - segmentsEnd;
            _notEncodedBuffer = new byte[notEncodedCount];
            for (int i = 0; i < notEncodedCount; i++)
            {
                _notEncodedBuffer[i] = soundBuffer[segmentsEnd + i];
            }

            for (int i = 0; i < segmentCount; i++)
            {
                byte[] segment = new byte[byteCap];
                for (int j = 0; j < segment.Length; j++)
                    segment[j] = soundBuffer[(i * byteCap) + j];
                int len;
                byte[] buff = _encoder.Encode(segment, segment.Length, out len);
                _bytesSent += (ulong)len;
                buff = _decoder.Decode(buff, len, out len);
                _playBuffer.AddSamples(buff, 0, len);
            }
        }

        private void StopEncoding()
        {
            _timer.Stop();


            _waveIn.StopRecording();
            _waveIn.Dispose();
            _waveIn = null;
            _waveOut.Stop();
            _waveOut.Dispose();
            _waveOut = null;
            _playBuffer = null;
            _encoder.Dispose();
            _encoder = null;
            _decoder.Dispose();
            _decoder = null;
        }
    }
}