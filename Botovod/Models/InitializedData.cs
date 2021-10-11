using System;
using System.Collections.Generic;
using System.Text;

namespace Botovod.Models
{
    /// <summary>
    /// Храним данные для инициализации нашей модели
    /// </summary>
    public class InitializedData
    {
        string kData = string.Empty;
        string sData = string.Empty;
        public InitializedData()
        {
            kData = Properties.Settings.Default.kData;
            sData = Properties.Settings.Default.sData;
        }

        // эквивалентно public string GetKData { get { return GetKData; } }
        public string GetKData => kData;
        public string GetSData => sData;

        public bool SetKData(string inData)
        {
            kData = inData;
            Properties.Settings.Default.kData = inData; // Записываем содержимое inData в kData
            Properties.Settings.Default.Save(); // Сохраняем переменные.
            return true;
        }

        public bool SetSData(string inData)
        {
            sData = inData;
            Properties.Settings.Default.sData = inData; // Записываем содержимое inData в kData
            Properties.Settings.Default.Save(); // Сохраняем переменные.
            return true;
        }
    }
}
