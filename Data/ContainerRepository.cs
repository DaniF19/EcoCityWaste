using EcoCityWaste.Models;
using System.Linq;

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

        public static Container GetById(int id)
        {
            return _containers.FirstOrDefault(c => c.Id == id);
        }

        public static void Update(Container updatedContainer)
        {
            var container = GetById(updatedContainer.Id);

            if (container == null)
                return;

            container.Location = updatedContainer.Location;
            container.Type = updatedContainer.Type;
            container.Status = updatedContainer.Status;
        }

    }
}