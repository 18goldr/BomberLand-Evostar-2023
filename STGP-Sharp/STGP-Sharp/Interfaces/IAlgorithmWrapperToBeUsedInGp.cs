using STGP_Sharp;

namespace GP
{
    public interface IAlgorithmResultWrapperToBeUsedWithGp
    {
    }
    
    public interface IAlgorithmWrapperToBeUsedWithGp
    {
        public IAlgorithmResultWrapperToBeUsedWithGp GetResult(GpFieldsWrapper gpFieldsWrapper);
    }


}