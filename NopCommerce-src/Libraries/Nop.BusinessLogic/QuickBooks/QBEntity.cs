﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NopSolutions.NopCommerce.BusinessLogic.Orders;
using NopSolutions.NopCommerce.BusinessLogic.CustomerManagement;

namespace NopSolutions.NopCommerce.BusinessLogic.QuickBooks
{
    /// <summary>
    /// Represents an QuickBooks entity
    /// </summary>
    public partial class QBEntity : BaseEntity
    {
        #region Ctor
        /// <summary>
        /// Creates a new instance of the QBEntity class
        /// </summary>
        public QBEntity()
        {
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the entity ID
        /// </summary>
        public int EntityId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the QuickBooks entity ID
        /// </summary>
        public string QBEntityId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the entity type
        /// </summary>
        public int EntityTypeId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the nopCommerce entity ID
        /// </summary>
        public int NopEntityId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the synchronization state
        /// </summary>
        public int SynStateId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the edit sequence number
        /// </summary>
        public string SeqNum
        {
            get;
            set;
        }
        
        /// <summary>
        /// Gets or sets the date and time when entity was created
        /// </summary>
        public DateTime CreatedOn
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the date and time when entity was updated
        /// </summary>
        public DateTime UpdatedOn
        {
            get;
            set;
        }
        #endregion

        #region Custom Properties
                /// <summary>
        /// Gets or sets the entity type
        /// </summary>
        public EntityTypeEnum EntityType
        {
            get
            {
                return (EntityTypeEnum)EntityTypeId;
            }
            set
            {
                EntityTypeId = (int)value;
            }
        }

        /// <summary>
        /// Gets or sets the synchronization state
        /// </summary>
        public SynStateEnum SynState
        {
            get
            {
                return (SynStateEnum)SynStateId;
            }
            set
            {
                SynStateId = (int)value;
            }
        }

        /// <summary>
        /// Gets the nopCommerce entity by EntityType and NopEntityId
        /// </summary>
        public BaseEntity NopEnity
        {
            get
            {
                switch(EntityType)
                {
                    case EntityTypeEnum.ReceivePayment:
                    case EntityTypeEnum.Invoice:
                    case EntityTypeEnum.TxnDel:
                    case EntityTypeEnum.TxnVoid:
                        return OrderManager.GetOrderById(NopEntityId);
                    case EntityTypeEnum.Customer:
                        return CustomerManager.GetCustomerById(NopEntityId);
                    default:
                        return null;
                }
            }
        }
        #endregion  
    }
}
