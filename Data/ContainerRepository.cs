using EcoCityWaste.Models;

namespace EcoCityWaste.Data
{
    public static class ContainerRepository
    {
        private static List<Container> _containers = new List<Container>();
        private static int _nextId = 1;

        public static void Add(Container container)
        {
            container.Id = _nextId++;
            _containers.Add(container);
        }

        public static List<Container> GetAll()
        {
            return _containers;
        }
    }
}