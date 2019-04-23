using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Services.Database
{
    public interface IKurumiDatabase
    {
        /// <summary>
        /// Loading the database from disk
        /// </summary>
        void Load();
        /// <summary>
        /// Saving the database to disk
        /// </summary>
        void Save(bool Show);
    }
}