using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qq          // ← проверьте, чтобы был тот же namespace, что и в сгенерённых классах
{
    public partial class Companies   // partial, чтобы «дописать» к авто-сгенерированному классу
    {
        public override string ToString()
        {
            // Если name не пустой — покажем его, иначе fallback на base.ToString()
            return string.IsNullOrWhiteSpace(name) ? base.ToString() : name;
        }
    }
}