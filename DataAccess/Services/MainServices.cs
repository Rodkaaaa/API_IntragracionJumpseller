using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.Shared.Services
{
    public class MainServices
    {
        #region Instancias de Multivende

        public ClientFactory JumpSeller { get; set; } = new ClientFactory("https://api.jumpseller.com/");
        public ClientFactory ImgBB { get; set; } = new ClientFactory("https://api.imgbb.com/");
        public ClientFactory FullBikeAPI { get; set; } = new ClientFactory("https://fullbikeapi.andesindustrial.cl/");
        public ClientFactory Conexion { get; set; } = new ClientFactory("https://apicda.andesindustrial.cl/");

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

        public MainServices()
        {
            instance = this;
        }

        
    }
}
