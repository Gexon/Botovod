using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Text;

namespace Botovod.Models
{
    // какая конкретно монета и сколько ее
    public class CoinStack : BindableBase
    {
        public CoinStack(Coin coin, int amount)
        {
            Coin = coin;
            Amount = amount;
        }
        public Coin Coin { get; }

        private int _amount;
        public int Amount
        {
            get { return _amount; }
            set { SetProperty(ref _amount, value); }
        }

        internal bool PullOne()
        {
            if (Amount > 0)
            {
                --Amount;
                return true;
            }
            return false;
        }

        internal void PushOne() => ++Amount;
    }

    // структура, а не класс, чтобы сравнение была сразу по значению, а не по ссылке
    // какая вообще бывает монета, все ее виды
    public struct Coin
    {
        //представим, что список пришел из базы данных
        public static readonly IReadOnlyList<Coin> Banknotes = new[] {
            new Coin("Биток", 1, true),
            new Coin("Два битка", 2, true),
            new Coin("Пять битков", 5, true),
            new Coin("Десять битков", 10, false),
            new Coin("Пятьдесят битков", 50, false),
            new Coin("Сто битков", 100, false),
        };
        private Coin(string name, int nominal, bool isCoin)
        {
            Name = name;
            Nominal = nominal;
            IsCoin = isCoin;
        }
        public string Name { get; }
        public int Nominal { get; }
        public bool IsCoin { get; } // монета ли это.
    }
}
