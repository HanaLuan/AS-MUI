using System;
using System.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

namespace Eruru.CSharp.Api {

	public class JsonSchemaDateTimeGenerationProvider : JSchemaGenerationProvider {

		public override bool CanGenerateSchema (JSchemaTypeGenerationContext context) {
			return context.ObjectType == typeof (DateTime);
		}

		public override JSchema GetSchema (JSchemaTypeGenerationContext context) {
			var jSchema = new JSchema {
				Title = context.SchemaTitle,
				Description = context.SchemaDescription,
				Type = JSchemaType.String
			};
			var defaultValue = context.MemberProperty?.DefaultValue;
			if (defaultValue != null) {
				jSchema.Default = EruruApi.ToDateTime (defaultValue);
			}
			var attributes = context.MemberProperty.AttributeProvider.GetAttributes (typeof (JsonSchemaDateTimeAttribute), true);
			var attribute = attributes.FirstOrDefault () as JsonSchemaDateTimeAttribute;
			switch (attribute?.Type) {
				default:
				case JsonSchemaDateTimeType.DateTime:
					jSchema.Format = "date-time";
					break;
				case JsonSchemaDateTimeType.Date:
					jSchema.Format = "date";
					break;
				case JsonSchemaDateTimeType.Time:
					jSchema.Format = "time";
					break;
			}
			return jSchema;
		}

	}

}