/**
	Forma de Utilzacion...
            string filename = "";
            SelcalGen selcal = new SelcalGen() { SampleRate = 8000, Gain=0.7 };
            if (selcal.Generate("A", "S", "B", "K", out filename) == true)
                Console.WriteLine("SelCal generado {0} ...",filename);
            else
                Console.WriteLine("Error al generar SelCal...");
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace U5ki.Infrastructure
{
    public class SelcalGen
    {
        /// <summary>
        /// 
        /// </summary>
        protected class SineOccilator 
        {
            private double _radiansPerCircle = Math.PI * 2;
            private double _currentFrequency = 2000;
            private double _sampleRate = 44100;
            private double _gain = 1;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="sampleRate"></param>
            public SineOccilator(double sampleRate, double gain)
            {
                _sampleRate = sampleRate;
                _gain = gain;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="value"></param>
            public void SetFrequency(double value)
            {
                _currentFrequency = value;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="sampleNumberInSecond"></param>
            /// <returns></returns>
            public double GetNext(int sampleNumberInSecond)
            {
                double samplesPerOccilation = (_sampleRate / _currentFrequency);
                double depthIntoOccilations = (sampleNumberInSecond % samplesPerOccilation) / samplesPerOccilation;
                return _gain * Math.Sin(depthIntoOccilations * _radiansPerCircle);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected class Silencio
        {
            public static double GetNext(int sampleNumberInSeconnd)
            {
                return 0;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sampleData"></param>
        /// <param name="sampleCount"></param>
        /// <param name="samplesPerSecond"></param>
        protected void SaveIntoStream(string filename, double[] sampleData, long sampleCount, int samplesPerSecond)
        {            // Export
            FileStream stream = File.Create(filename);
            System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream);
            int RIFF = 0x46464952;
            int WAVE = 0x45564157;
            int formatChunkSize = 16;
            int headerSize = 8;
            int format = 0x20746D66;
            short formatType = 1;
            short tracks = 1;
            short bitsPerSample = 16;
            short frameSize = (short)(tracks * ((bitsPerSample + 7) / 8));
            int bytesPerSecond = samplesPerSecond * frameSize;
            int waveSize = 4;
            int data = 0x61746164;
            int samples = (int)sampleCount;
            int dataChunkSize = samples * frameSize;
            int fileSize = waveSize + headerSize + formatChunkSize + headerSize + dataChunkSize;
			
            writer.Write(RIFF);
            writer.Write(fileSize);
            writer.Write(WAVE);
            writer.Write(format);
            writer.Write(formatChunkSize);
            writer.Write(formatType);
            writer.Write(tracks);
            writer.Write(samplesPerSecond);
            writer.Write(bytesPerSecond);
            writer.Write(frameSize);
            writer.Write(bitsPerSample);
            writer.Write(data);
            writer.Write(dataChunkSize);

            double sample_l;
            short sl;
            for (int i = 0; i < sampleCount; i++)
            {
                sample_l = sampleData[i] * 30000.0;
                if (sample_l < -32767.0f) { sample_l = -32767.0f; }
                if (sample_l > 32767.0f) { sample_l = 32767.0f; }
				
                sl = (short)sample_l;
                stream.WriteByte((byte)(sl & 0xff));
                stream.WriteByte((byte)(sl >> 8));
                //stream.WriteByte((byte)(sl & 0xff));
                //stream.WriteByte((byte)(sl >> 8));
            }
            stream.Close();
        }

        /// <summary>
        /// Mapa de Frecuencias...
        /// </summary>
        protected Dictionary<string, double> frecuencias = new Dictionary<string, double>()
        {
            {"A",312.6},
            {"B",346.7},
            {"C",384.6},
            {"D",426.6},
            {"E",473.2},
            {"F",524.8},
            {"G",582.1},
            {"H",645.7},
            {"J",716.1},
            {"K",794.3},
            {"L",881.0},
            {"M",977.2},
            {"P",1083.9},
            {"Q",1202.3},
            {"R",1333.5},
            {"S",1479.1}
        };

        /// <summary>
        /// 
        /// </summary>
        public int SampleRate { get; set; }
        public double Gain { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code1"></param>
        /// <param name="code2"></param>
        /// <param name="code3"></param>
        /// <param name="code4"></param>
        /// <returns></returns>
        public bool Generate(string code1, string code2, string code3, string code4, out string filename)
        {
            double f1,f2,f3,f4;
            filename = "";
            if (frecuencias.TryGetValue(code1, out f1) == false ||
                 frecuencias.TryGetValue(code2, out f2) == false ||
                frecuencias.TryGetValue(code3, out f3) == false ||
                frecuencias.TryGetValue(code4, out f4) == false)
                return false;

            if (code1.CompareTo(code2) >= 0 ||
                code3.CompareTo(code4) >= 0)        
                return false;

            if (code1 == code3 && code2 == code4)
                return false;

            List<double> data = new List<double>();

            SineOccilator osc1 = new SineOccilator(SampleRate, Gain);
            SineOccilator osc2 = new SineOccilator(SampleRate, Gain);

            for (int i = 0; i < SampleRate / 10; i++)       // 1/5 segundo....
                data.Add(Silencio.GetNext(i));

            osc1.SetFrequency(f1); 
            osc2.SetFrequency(f2);

            for (int i = 0; i < SampleRate * 1; i++)       // 1 segundos...
            {
                double sample = osc1.GetNext(i) + osc2.GetNext(i);
                data.Add(sample / 2);
            }

            for (int i = 0; i < SampleRate / 5; i++)       // 1/5 segundo....
                data.Add(Silencio.GetNext(i));

            osc1.SetFrequency(f3);
            osc2.SetFrequency(f4);

            for (int i = 0; i < SampleRate * 1; i++)       // 1 segundos---
            {
                double sample = osc1.GetNext(i) + osc2.GetNext(i);
                data.Add(sample / 2);
            }

            for (int i = 0; i < SampleRate / 10; i++)       // 1/5 segundo....
                data.Add(Silencio.GetNext(i));

            filename = String.Format("sc_{0}{1}-{2}{3}.wav", code1, code2, code3, code4);
            SaveIntoStream(filename, data.ToArray(), data.Count, SampleRate);
            return true;
        }
    }
}
