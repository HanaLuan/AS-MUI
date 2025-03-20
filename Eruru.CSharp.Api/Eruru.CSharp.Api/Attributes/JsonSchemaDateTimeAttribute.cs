using System;

namespace Eruru.CSharp.Api {

	[AttributeUsage (AttributeTargets.Property | AttributeTargets.Field)]
	public class JsonSchemaDateTimeAttribute : Attribute {

		public JsonSchemaDateTimeType Type { get; set; } = JsonSchemaDateTimeType.DateTime;

		public JsonSchemaDateTimeAttribute (JsonSchemaDateTimeType type) {
			Type = type;
		}

	}

}