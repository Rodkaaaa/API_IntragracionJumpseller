using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataAccess.Shared.Services
{
    public class MainServices
    {
        #region Instancias de Multivende
        [Required]
        public static ClientFactory JumpSeller { get; set; } = new ClientFactory("https://api.jumpseller.com/");
        [Required]
        public static ClientFactory ImgBB { get; set; } = new ClientFactory("https://api.imgbb.com/");
        [Required]
        public static ClientFactory FullBikeAPI { get; set; } = new ClientFactory("https://fullbikeapi.andesindustrial.cl/");
        [Required]
        public static ClientFactory Conexion { get; set; } = new ClientFactory("https://apicda.andesindustrial.cl/");

        #endregion

        private static MainServices instance;
        public static MainServices GetInstance()
        {
            if (instance == null)
            {
                return new MainServices();
            }

            return instance;
        }

        public  MainServices()
        {
            instance = this;
        }

        
    }
}
