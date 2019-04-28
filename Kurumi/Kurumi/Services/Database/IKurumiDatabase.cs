using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Services.Database
{
    public interface IKurumiDatabase
    {
        void Load();
        void Save(bool Show);
    }
}