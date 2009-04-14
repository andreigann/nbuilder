﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FizzWare.NBuilder.Implementation;
using FizzWare.NBuilder.PropertyNaming;

namespace FizzWare.NBuilder.Implementation
{
    public class ListBuilder<T> : IListBuilderImpl<T>
    {
        private readonly int size;
        private readonly IPropertyNamer<T> propertyNamer;
        private readonly IReflectionUtil reflectionUtil;
        private readonly T[] mainList;
        private readonly DeclarationQueue<T> declarations;

        public virtual int Capacity
        {
            get
            {
                return size;
            }
        }

        public IDeclarationQueue<T> Declarations
        {
            get { return declarations; }
        }

        public ListBuilder(int size, IPropertyNamer<T> valuePropertyNamer, IReflectionUtil reflectionUtil)
        {
            this.size = size;
            this.propertyNamer = valuePropertyNamer;
            this.reflectionUtil = reflectionUtil;

            mainList = new T[size];

            declarations = new DeclarationQueue<T>(size);
        }

        public IObjectBuilder<T> CreateObjectBuilder()
        {
            return new ObjectBuilder<T>(reflectionUtil);
        }

        public IOperable<T> WhereAll()
        {
            declarations.Enqueue(new GlobalDeclaration<T>(this, CreateObjectBuilder()));
            return (IOperable<T>)declarations.Peek();
        }

        public void Construct()
        {
            if (declarations.GetDistinctAffectedItemCount() < this.Capacity &&
                !declarations.ContainsGlobalDeclaration() &&
                reflectionUtil.RequiresConstructorArgs(typeof(T))
                )
            {
                throw new BuilderException(
                    @"No WhereAll() was specified and the total affected item count 
                                            of all of the other operations was less than the capacity of the list");
            }

            if (declarations.GetDistinctAffectedItemCount() < this.Capacity && !declarations.ContainsGlobalDeclaration())
            {
                var globalDeclaration = new GlobalDeclaration<T>(this, CreateObjectBuilder());
                declarations.Enqueue(globalDeclaration);
            }

            declarations.Construct();
        }

        public IList<T> Name(IList<T> list)
        {
            propertyNamer.SetValuesOfAllIn(list);
            return list;
        }

        public IList<T> Build()
        {
            Construct();
            declarations.AddToMaster(mainList);

            var list = mainList.ToList();

            propertyNamer.SetValuesOfAllIn(list);
            declarations.CallFunctions(list);

            return list;
        }

        public IDeclaration<T> AddDeclaration(IDeclaration<T> declaration)
        {
            this.declarations.Enqueue(declaration);
            return declarations.Peek();
        }
    }
}