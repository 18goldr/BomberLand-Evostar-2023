// ReSharper disable NotAccessedField.Global
namespace STGP_Sharp.Utilities.GP
{
    // TODO ideally we don't want anyone doing anything with these besides incrementing or resetting them.
    public class VerboseInfo
    {
        private bool _verbose;
        
        public int numberOfTimesCrossoverSkipped;

        public int numberOfTimesCrossoverSwappedEquivalentNode;

        public int numberOfTimesCrossoverWasTooDeep;

        public int numberOfTimesMutationCreatedEquivalentNode;

        public int numberOfTimesMutationSkipped;

        public int numberOfTimesNoLegalCrossoverPoints;

        public static implicit operator bool(VerboseInfo v)
        {
            return v._verbose;
        }

        public static implicit operator VerboseInfo(bool v)
        {
            return new VerboseInfo { _verbose = v };
        }

        public void ResetCountInfo()
        {
            numberOfTimesCrossoverWasTooDeep = 0;
            numberOfTimesCrossoverSkipped = 0;
            numberOfTimesMutationCreatedEquivalentNode = 0;
            numberOfTimesMutationSkipped = 0;
            numberOfTimesCrossoverSwappedEquivalentNode = 0;
            numberOfTimesNoLegalCrossoverPoints = 0;
        }
    }
}