using System;

namespace ActivityWatch.API.V1
{
    public class EventDataAppEditorActivity : IEventData
    {
        #region Constructors

        public EventDataAppEditorActivity()
        {
        }

        #endregion Constructors

        #region Properties

        [Newtonsoft.Json.JsonIgnore]
        public string BucketIDCustomPart { get => "aw-visualstudio-editor"; }

        [Newtonsoft.Json.JsonProperty("caller", Required = Newtonsoft.Json.Required.Default)]
        public string Caller { get; set; }

        [Newtonsoft.Json.JsonProperty("file", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public string File { get; set; }

        [Newtonsoft.Json.JsonProperty("language", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public string Language { get; set; }

        [Newtonsoft.Json.JsonProperty("project", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public string Project { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public string TypeName { get => "app.editor.activity"; }

        #endregion Properties

        #region Methods

        public override bool Equals(object obj)
        {
            if (obj is EventDataAppEditorActivity data)
            {
                return this.TypeName == data.TypeName
                    && this.BucketIDCustomPart == BucketIDCustomPart
                    && this.File == data.File
                    && this.Language == data.Language
                    && this.Project == data.Project
                    && this.Caller == data.Caller;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Tuple.Create(
                this.TypeName,
                this.BucketIDCustomPart,
                this.File,
                this.Language,
                this.Project,
                this.Caller
            ).GetHashCode();
        }

        #endregion Methods
    }
}