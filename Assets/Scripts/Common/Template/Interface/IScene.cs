namespace Common.Template.Interface
{
    public interface ISceneParameter<in T>
    {
        void SetParameter(T parameter);
    }
}
