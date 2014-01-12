/*

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
*/
using System.Diagnostics.Contracts;


namespace cointoss.Models
{
    public class Token
    {
        public Token(){ }

        public string _betid;
        public string BetID { get { return this._betid; } set { this._betid = value;} }

        public string _cost;
        public string Cost { get { return this._cost; } set { this._cost = value; } }

        public string _result;
        public string EffectiveResult { get {return this._result;}  set {this._result = value;} }

        public int _id;
        public int ID { get { return this._id; } set { this._id = value; } }

        public string _guess;
        public string InitialGuess { get { return this._guess; } set { this._guess = value; } }

        public string _token;
        public string OAuthToken { get { return this._token; } set { this._token = value; } }
    }

    public class Bet
    {
        public Bet() { }

        public int _amount;
        public int amount { get { return this._amount; } set { this._amount = value; } }

        public string _guess;
        public string guess { get { return this._guess; } set { this._guess = value; } }

        public int _id;
        public int ID { get { return this._id; } set { this._id = value; } }
    }
}