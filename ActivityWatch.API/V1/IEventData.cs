namespace ActivityWatch.API.V1
{
    public interface IEventData
    {
        #region Methods

        bool Equals(object obj);

        int GetHashCode();

        #endregion Methods

        #region Properties

        string BucketIDCustomPart { get; }
        string TypeName { get; }

        #endregion Properties
    }
}