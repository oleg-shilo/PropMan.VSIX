// Guids.cs
// MUST match guids.h
using System;

namespace OlegShilo.PropMan
{
    static class GuidList
    {
        public const string guidPropManPkgString = "7c64986f-f4d6-4983-a49a-518d5512b212";
        public const string guidPropManCmdSetString = "db2d94ca-ca87-4240-99ea-931d7f8e7701";
        public const string guidPropManCmdConfigSetString = "db2d94ca-ca87-4240-99ea-931d7f8e7733";
        public const string guidToolWindowPersistanceString = "fd0da38f-47b9-4072-b27d-01629b580799";

        public static readonly Guid guidPropManCmdSet = new Guid(guidPropManCmdSetString);
        public static readonly Guid guidPropManConfigCmdSet = new Guid(guidPropManCmdConfigSetString);
    };
}