namespace Common.Template.Interface
{
    public interface IUpdateable
    {
        void OnUpdate();
    }

    public interface IFixedUpdateable
    {
        void OnFixedUpdate();
    }
}