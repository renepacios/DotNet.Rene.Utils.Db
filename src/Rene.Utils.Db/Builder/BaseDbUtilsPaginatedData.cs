namespace Rene.Utils.Db.Builder
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class BaseDbUtilsPaginatedData<TEntity> : IDbUtilsPaginatedData<TEntity>
    {
        public int TotalPages { get; set; }
        public int TotalElements { get; set; }
        public int Size { get; set; }
        public int Number { get; set; }
        public int NumberOfElements { get; set; }
        public bool First { get; set; }
        public bool Last { get; set; }
        public bool Empty { get; set; }
        public IList<TEntity> Content { get; set; }

        /// <summary>
        /// Construye el objeto paginado
        /// </summary>
        /// <param name="content">Enviar 1 más en el listado para calcular si hay más páginas </param>
        /// <param name="pageNumber">Número de página mostrada</param>
        /// <param name="size">Tamaño del conjunto de registros para saber si hay más páginas</param>
        /// <param name="totalElements">Si la pagination lleva contador de páginas indicar aquí el número total de registros</param>
        public BaseDbUtilsPaginatedData(IList<TEntity> content, int pageNumber, int size, int? totalElements = null)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than zero");

            Number = pageNumber;
            Size = size;

            if (totalElements.HasValue)
            {
                TotalElements = totalElements.Value;
                TotalPages = (TotalElements / size) + (TotalElements % size != 0 ? 1 : 0);
            }

            var numRegistrosEnOrigen = content.Count;

            First = 0==pageNumber;
            Empty = 0 == numRegistrosEnOrigen;
            Last = numRegistrosEnOrigen <= size; //si no hay más registros que el tamaño de la página estamos en la última ya que trae siempre 1 registro de más para ver si hay más datos


            Content = content.Take(size).ToList();
            NumberOfElements = Content.Count;

        }

    }


}
