﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace ENode.EventStore.MongoDb.Models
{
    public class EventStream
    {
        public string AggregateRootId { get; set; }

        public string AggregateRootTypeName { get; set; }

        public string CommandId { get; set; }

        public DateTime CreatedOn { get; set; }

        public string Events { get; set; }

        public ObjectId Id { get; set; }

        public int Version { get; set; }
    }
}