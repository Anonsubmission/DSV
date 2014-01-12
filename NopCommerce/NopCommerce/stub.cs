/*

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
*/
using System.Diagnostics.Contracts;

namespace System
{
	public struct Decimal
	{	
		int _dummy;
		/*
		public Decimal(){
			_dummy = 0;
		}
		
		public Decimal(Decimal val){
			Contract.Assert(false);
			_dummy = val._dummy;
		}
		*/
		public static bool operator ==(Decimal d1, Decimal d2) {
			//return true;
            
			if (d1._dummy!=d2._dummy) 
                return false;
            return true;
        
		}
		
		public static bool operator !=(Decimal d1, Decimal d2) {
			//return false;
            if (d1._dummy!=d2._dummy) 
                return true;
            return false;
        
		}
	}
}

namespace NopSolutions.NopCommerce.BusinessLogic.Orders
{
	using System;
    public partial class Order : BaseEntity
    {	
        public int _id;
        public Decimal _total;
		public Order(){
			_total = new Decimal();
		}
        public int OrderId { get { return this._id; } set { this._id = value; } }
        public Decimal OrderTotal { get { return this._total; } set { this._total = value; } }
    }

	public partial class OrderManager
    {
		public static bool CanMarkOrderAsPaid(Order order ){
			return true;
		}
		
	}
}
