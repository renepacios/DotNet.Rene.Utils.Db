
namespace Rene.Utils.Db.Builder
{
    using AutoMapper;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using Rene.Utils.Db;
    using Rene.Utils.Db.Commands;
    using Rene.Utils.Db.DbInternal;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal class GenericCommandHandler<TViewModel, TModel, TDbContext, Tuow> :
        IRequestHandler<AddCommand<TViewModel>, TViewModel>,
        IRequestHandler<UpdateCommand<TViewModel>, TViewModel>,
        IRequestHandler<GetAllCommand<TViewModel>, IEnumerable<TViewModel>>,
        IRequestHandler<GetCommand<TViewModel>, TViewModel>,
        IRequestHandler<DeleteCommand<TViewModel>, bool>,
        IRequestHandler<GetBySpecCommand<TViewModel, TModel>, IReadOnlyList<TViewModel>>,
        IRequestHandler<GetPaginatedCommand<TViewModel, TModel>, IDbUtilsPaginatedData<TViewModel>>

        //  where TViewModel : IWithGenericHandler<TModel>
        where TDbContext : DbContext
        where TModel : class
        where Tuow : IDbUtilsUnitOfWork
    {
        private readonly IMapper _mapper;

        private readonly TDbContext _dbContext;
        private readonly Tuow _uow;

        private readonly DbSet<TModel> _db;
        //   private readonly ISimpleCrudServiceAsync<TModel> _db;

        public GenericCommandHandler(IMapper mapper, TDbContext dbContext, Tuow uow)
        {
            _mapper = mapper;
            _dbContext = dbContext;
            _uow = uow;
            _db = _dbContext.Set<TModel>();

            //ISimpleCrudServiceAsync<TModel> service    _db = service;
        }

        public async Task<TViewModel> Handle(AddCommand<TViewModel> request, CancellationToken cancellationToken)
        {
            var model = _mapper.Map<TModel>(request.ViewModel);
            // var model= await _db.AddAsync(new TModel());

            var entityEntry = await _db.AddAsync(model, cancellationToken);

            await SaveChanges(cancellationToken);

            //   await _dbContext.SaveChangesAsync(cancellationToken);

            return _mapper.Map<TViewModel>(entityEntry?.Entity);
            //  return request.ViewModel;
        }

        public async Task<TViewModel> Handle(UpdateCommand<TViewModel> request, CancellationToken cancellationToken)
        {
            //var model = await _db.FindAsync(request.Id, cancellationToken);
            var model = await _db.FindAsync(request.Id);

            if (model == null) throw new KeyNotFoundException($"Entidad no encontrada {request.Id}");


            _mapper.Map<TViewModel, TModel>(request.ViewModel, model);


            //Por si en el viewmodel envían la Pk a null como no puedo hacer inferencia del tipo jugamos un poquito con el esquema del dbcontext
            //var keyName = _dbContext.Model
            //    .FindEntityType(typeof(TModel))
            //    .FindPrimaryKey().Properties
            //    .Select(x => x.Name)
            //    .SingleOrDefault();


            var keyName = GetKeyNameFromEntityType();


            if (!string.IsNullOrEmpty(keyName))
            {
                var pi = typeof(TModel).GetProperty(keyName);
                pi?.SetValue(model, request.Id);
            }

            //TODO: No es fiable lo que devuelve porque los datos que no se traen o son machacados en el map no siempre los devuelve el db.update
            //  - O devolver boleano
            //  - Trabajar con proyecciones para que modifique todo el grafo del viewmodel 

            var entityEntry = _db.Update(model);

            await SaveChanges(cancellationToken);

            //await _dbContext.SaveChangesAsync(cancellationToken);

            return _mapper.Map<TViewModel>(entityEntry?.Entity);

        }

        public async Task<IEnumerable<TViewModel>> Handle(GetAllCommand<TViewModel> request, CancellationToken cancellationToken)
        {
            //var dev = await _db.AsNoTracking()

            var dev = await _mapper
                .ProjectTo<TViewModel>(_db.AsNoTracking())
                .ToListAsync(cancellationToken: cancellationToken);

            return dev;
            //return dev.Select(m => _mapper.Map<TViewModel>(m));
        }

        public async Task<TViewModel> Handle(GetCommand<TViewModel> request, CancellationToken cancellationToken)
        {
            var dev = await _db.FindAsync(request.Id);

            if (dev == null) throw new KeyNotFoundException($"Entidad no encontrada {request.Id}");

            return _mapper.Map<TViewModel>(dev);
        }

        public async Task<bool> Handle(DeleteCommand<TViewModel> request, CancellationToken cancellationToken)
        {
            var result = await _db.FindAsync(request.Id);

            if (result == null) throw new KeyNotFoundException($"Entidad no encontrada {request.Id})");

            var entityEntry = _db.Remove(result);

            //  await _dbContext.SaveChangesAsync(cancellationToken);

            await SaveChanges(cancellationToken);

            return true;
        }

        public async Task<IReadOnlyList<TViewModel>> Handle(GetBySpecCommand<TViewModel, TModel> request, CancellationToken cancellationToken)
        {
            IQueryable<TModel> data = SpecificationEvaluator<TModel>.GetQuery(_db, request.Specification);

            var dev = await _mapper
                .ProjectTo<TViewModel>(data.AsNoTracking())
                .ToListAsync(cancellationToken: cancellationToken);

            return dev;

            //return _mapper.Map<IReadOnlyList<TViewModel>>(data);


        }

        public async Task<IDbUtilsPaginatedData<TViewModel>> Handle(GetPaginatedCommand<TViewModel, TModel> request, CancellationToken cancellationToken)
        {
            if (request.Specification != null && request.Specification.IsPagingEnabled)
            {
                throw new ArgumentException("Specification must be without pagination settings");
            }

            //Hacemos que pida 1 registro de más para calcular si hay más páginas de registro 
            var registrosApedir = request.Size + 1;

            //Contemplamos si pide registros paginados sin filtros, el count será el total de registros
            var query = request.Specification == null
                        ? _db.AsNoTracking()
                        : SpecificationEvaluator<TModel>.GetQuery(_db.AsNoTracking(), request.Specification);


            var totalRegistros = await query.CountAsync(cancellationToken: cancellationToken);

            var queryResult = query
                .Skip(request.Size * request.PageNumber)
                .Take(registrosApedir);


            var registros = await _mapper
                .ProjectTo<TViewModel>(queryResult)
                .ToListAsync(cancellationToken: cancellationToken);

            return new BaseDbUtilsPaginatedData<TViewModel>(registros, request.PageNumber, request.Size, totalRegistros);

        }


        private async Task<int> SaveChanges(CancellationToken cancellationToken)
        {
            if (_uow==null || _uow.GetType().IsAssignableFrom(typeof(FakeUnitOfWork<>)))
            {
                return await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return await _uow.SaveChangesAsync(cancellationToken);

        }


        private string GetKeyNameFromEntityType()
        {
            if (_uow is FakeUnitOfWork<TDbContext> fakeUow)
            {
                return fakeUow.GetKeyNameFromEntityType<TModel>();
            }
            return _dbContext.GetKeyNameFromEntityType<TModel>();
        }

    }


}