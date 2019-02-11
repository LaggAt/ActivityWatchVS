using System;

namespace ActivityWatch.API.V1
{
    public partial class Event
    {
        #region Constructors

        public Event()
        {
        }

        #endregion Constructors

        #region Methods

        public override bool Equals(object obj)
        {
            if (obj is Event evt)
            {
                return ((this.Data as EventDataAppEditorActivity)?.Equals(evt.Data as EventDataAppEditorActivity) ?? false);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Tuple.Create(
                this.Data as EventDataAppEditorActivity
                ).GetHashCode();
        }

        #endregion Methods
    }
}