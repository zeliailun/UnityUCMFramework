using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public enum BusEventKind
    {
        Action,
        Query,
        QueryWithParam
    }

    public readonly struct BusEventKey : IEquatable<BusEventKey>
    {
        public readonly BusEventKind Kind;
        public readonly Type TypeA;
        public readonly Type TypeB;
        public readonly EntityId Id;
        public readonly bool HasId;

        private BusEventKey(
            BusEventKind kind,
            Type typeA,
            Type typeB,
            EntityId id,
            bool hasId)
        {
            Kind = kind;
            TypeA = typeA;
            TypeB = typeB;
            Id = id;
            HasId = hasId;
        }

        public static BusEventKey Action<TEvent>()
        {
            return new BusEventKey(
                BusEventKind.Action,
                typeof(TEvent),
                null,
                default,
                false);
        }

        public static BusEventKey Action<TEvent>(EntityId id)
        {
            return new BusEventKey(
                BusEventKind.Action,
                typeof(TEvent),
                null,
                id,
                true);
        }

        public static BusEventKey Query<TResult>()
        {
            return new BusEventKey(
                BusEventKind.Query,
                typeof(TResult),
                null,
                default,
                false);
        }

        public static BusEventKey Query<TResult>(EntityId id)
        {
            return new BusEventKey(
                BusEventKind.Query,
                typeof(TResult),
                null,
                id,
                true);
        }

        public static BusEventKey QueryWithParam<TQuery, TResult>()
        {
            return new BusEventKey(
                BusEventKind.QueryWithParam,
                typeof(TQuery),
                typeof(TResult),
                default,
                false);
        }

        public static BusEventKey QueryWithParam<TQuery, TResult>(EntityId id)
        {
            return new BusEventKey(
                BusEventKind.QueryWithParam,
                typeof(TQuery),
                typeof(TResult),
                id,
                true);
        }

        public bool Equals(BusEventKey other)
        {
            return Kind == other.Kind
                && TypeA == other.TypeA
                && TypeB == other.TypeB
                && HasId == other.HasId
                && EqualityComparer<EntityId>.Default.Equals(Id, other.Id);
        }

        public override bool Equals(object obj)
        {
            return obj is BusEventKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, TypeA, TypeB, Id, HasId);
        }
    }
}