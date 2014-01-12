﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web.Http.OData.TestCommon.Models;

namespace System.Web.Http.OData.Query.Controllers
{
    public class CustomerHighLevelController : ODataController
    {
        [Queryable]
        public IQueryable<Customer> Get()
        {
            throw new NotImplementedException();
        }

        [Queryable]
        public object GetObject()
        {
            // this can return Customer or BellevueCustomer
            throw new NotImplementedException();
        }

        [Queryable]
        public IEnumerable<BellevueCustomer> GetIEnumerableOfCustomer()
        {
            throw new NotImplementedException();
        }

        [Queryable]
        public Collection<SeattleCustomer> GetCollectionOfCustomer()
        {
            throw new NotImplementedException();
        }

        [Queryable]
        public List<RedmondCustomer> GetListOfCustomer()
        {
            throw new NotImplementedException();
        }

        [Queryable]
        public CustomerCollection GetStronglyTypedCustomer()
        {
            throw new NotImplementedException();
        }

        [Queryable]
        public Customer[] GetArrayOfCustomers()
        {
            throw new NotImplementedException();
        }

        [Queryable]
        public NonGenericEnumerable GetNonGenericEnumerable()
        {
            throw new NotImplementedException();
        }

        public IEnumerable GetNonQueryable()
        {
            throw new NotImplementedException();
        }

        public TwoGenericsCollection GetTwoGenericsCollection()
        {
            throw new NotImplementedException();
        }
    }

    public class CustomerCollection : IEnumerable<Customer>
    {
        List<Customer> _list;

        public CustomerCollection()
        {
            _list = new List<Customer>()
            {
                new Customer(){ Name = "D" },
                new Customer(){ Name = "E" },
                new Customer(){ Name = "F" },
            };
        }

        public IEnumerator<Customer> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        Collections.IEnumerator Collections.IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }

    public class TwoGenericsCollection : IMyInterface<Customer, RentonCustomer>
    {
    }

    public interface IMyInterface<T1, T2>
    {
    }
}
