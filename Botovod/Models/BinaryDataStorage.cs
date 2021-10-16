using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;

namespace Botovod.Models
{
    public static class BinaryDataStorage
    {
        
        /// <summary>
        /// Сохраняем список сделок на диск
        /// </summary>
        /// <param name="traderDeals">списо сделок</param>
        public static bool SaveDealsToDisk(ReadOnlyObservableCollection<BotovodDeal> traderDeals)
        {
            List<BotovodDeal> savedData = new List<BotovodDeal>();
            savedData.AddRange(traderDeals);
            // создаем объект BinaryFormatter
            BinaryFormatter formatter = new BinaryFormatter();
            // получаем поток, куда будем записывать сериализованный объект
            string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            using (FileStream fs = new FileStream(appDir + "\\trader_deals.btv", FileMode.OpenOrCreate))
            {
                formatter.Serialize(fs, savedData);
            }
            return true;
        }

        /// <summary>
        /// Загрузка списка сделок с диска
        /// </summary>
        /// <returns>Возвращяет список сделок</returns>
        public static List<BotovodDeal> LoadDealsFormDisk()
        {
            List<BotovodDeal> result = new List<BotovodDeal>();
            string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (File.Exists(appDir + "\\trader_deals.btv"))
            {
                // десериализация из файла 
                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream fs = new FileStream(appDir + "\\trader_deals.btv", FileMode.Open))
                {
                    try
                    {
                        result = (List<BotovodDeal>)formatter.Deserialize(fs);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки задач с диска {ex}", "LoadDealsFormDisk", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }

            return result;
        }

    }


}
