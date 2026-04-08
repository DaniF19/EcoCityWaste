using System;

namespace EcoCityWaste.Helpers
{
    public static class ContainerHelpers
    {
        public static string GetTypeIcon(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return "fa-trash";

            return type.ToLower() switch
            {
                "plástico" or "plastico" => "fa-recycle",
                "papel" or "cartão" or "cartao" => "fa-box-archive",
                "vidro" => "fa-wine-glass-empty",
                "indiferenciado" => "fa-trash-can",
                "orgânico" or "organico" => "fa-apple-whole",
                _ => "fa-trash"
            };
        }
    }
}
