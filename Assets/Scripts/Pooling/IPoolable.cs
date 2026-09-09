namespace Arena.Pooling
{
    public interface IPoolable
    {
        void OnSpawn();

        void OnDespawn();
    }
}
