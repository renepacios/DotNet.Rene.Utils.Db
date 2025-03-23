
/*
 *  TODO:
 *  - Ver integración con una implementación de UnitOfWork externa
 *  - Ver como generalizar la gestión de la implementación concreta del handler
 */


namespace Rene.Utils.Db.Builder
{
    using Commands;
    using Db;
    using MediatR;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    internal static class GenericCommandHandlersConfiguration
    {
        //TODO: Pasar los asemblies a escanear
        public static WebApplicationBuilder AddMediatRGenericHandlers(this WebApplicationBuilder builder, DbUtilsOptions options, Assembly assemblyToScan = null)
        {
            var services = builder.Services;

            Type filterSuperType = typeof(IWithGenericHandler<>); //change this type to select viewmodels with generic commandHandlers
            const string INTERFACE_NAME = "IWithGenericHandler";
            const string PROPERTY_MODEL_TYPE_NAME = "MapFromType";

            //opciones de arranque
            Type dbContextType = options.DbContextType;
            Type uowType = options.UnitOfWorkType;
            //TODO: Gestionar la opción de UnitOfWork


            var assembly = assemblyToScan ?? Assembly.GetCallingAssembly();//  typeof(ContainerBuilder).Assembly


            //Get viewmodel to implement generic handlers
            var viewModelsTypes = assembly.GetExportedTypes()
                .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == filterSuperType)) // typeof(IMapFrom<>)))
                .ToList();

            foreach (Type viewModelType in viewModelsTypes)
            {

                var instance = Activator.CreateInstance(viewModelType);

                var interfaceType = viewModelType.GetInterfaces().FirstOrDefault(w => w.Name.StartsWith(INTERFACE_NAME));


                // if (interfaceType == null) continue;
                var mapFromTypeProperty = interfaceType?.GetProperty(PROPERTY_MODEL_TYPE_NAME);
                if (mapFromTypeProperty == null) continue;


                Type modelType = mapFromTypeProperty.GetValue(instance, null) as Type;

                if (modelType == null) continue;



                Type GetCommandHandler()
                    => typeof(GenericCommandHandler<,,,>).MakeGenericType(viewModelType, modelType, dbContextType, uowType);

                //Método Helper para no repetir código en los comandos que retornan el viewModel
                void RegisterCommandHandler(Type commandType)
                {
                    var interfaceHandler = typeof(IRequestHandler<,>).MakeGenericType(commandType, viewModelType);
                    var concreteHandlerType = GetCommandHandler(); // typeof(GenericCommandHandler<,,>).MakeGenericType(viewModelType, modelType, dbContextType);
                    services.AddScoped(interfaceHandler, concreteHandlerType);
                }

                //Add
                var addCommandType = typeof(AddCommand<>).MakeGenericType(viewModelType);
                RegisterCommandHandler(addCommandType);

                //Update
                var updateCommandType = typeof(UpdateCommand<>).MakeGenericType(viewModelType);
                RegisterCommandHandler(updateCommandType);

                //GetOne
                var getCommandType = typeof(GetCommand<>).MakeGenericType(viewModelType);
                RegisterCommandHandler(getCommandType);

                //GetAll
                var getAllCommandType = typeof(GetAllCommand<>).MakeGenericType(viewModelType);
                var iEnumerableViewModelType = typeof(IEnumerable<>).MakeGenericType(viewModelType);
                var interfaceGetAllCommandHandlerType = typeof(IRequestHandler<,>).MakeGenericType(getAllCommandType, iEnumerableViewModelType);
                var concreteGetAllCommandHandlerType = GetCommandHandler(); //  typeof(GenericCommandHandler<,,>).MakeGenericType(viewModelType, modelType, dbContextType);
                services.AddScoped(interfaceGetAllCommandHandlerType, concreteGetAllCommandHandlerType);


                //Delete
                var deleteCommandType = typeof(DeleteCommand<>).MakeGenericType(viewModelType);
                var interfaceDeleteCommandHandlerType = typeof(IRequestHandler<,>).MakeGenericType(deleteCommandType, typeof(bool));
                var concreteDeleteCommandHandlerType = GetCommandHandler(); //  typeof(GenericCommandHandler<,,>).MakeGenericType(viewModelType, modelType, dbContextType);
                services.AddScoped(interfaceDeleteCommandHandlerType, concreteDeleteCommandHandlerType);


                //GetFilter
                var getFilterCommandType = typeof(GetBySpecCommand<,>).MakeGenericType(viewModelType, modelType);
                var iReadOnlyViewModelType = typeof(IReadOnlyList<>).MakeGenericType(viewModelType);
                var interfaceGetFilterCommandHandlerType = typeof(IRequestHandler<,>).MakeGenericType(getFilterCommandType, iReadOnlyViewModelType);
                var concreteGetFilterCommandHandlerType = GetCommandHandler(); //  typeof(GenericCommandHandler<,,>).MakeGenericType(viewModelType, modelType, dbContextType);
                services.AddScoped(interfaceGetFilterCommandHandlerType, concreteGetFilterCommandHandlerType);


                //GetFiltrPaginado
                var getPaginatedCommandType = typeof(GetPaginatedCommand<,>).MakeGenericType(viewModelType, modelType);
                var iPaginatedDataViewModelType = typeof(IDbUtilsPaginatedData<>).MakeGenericType(viewModelType);
                var interfacePaginatedCommandType = typeof(IRequestHandler<,>).MakeGenericType(getPaginatedCommandType, iPaginatedDataViewModelType);
                // var concreteGetFilterCommandHandlerType = typeof(GenericCommandHandler<,>).MakeGenericType(viewModelType, modelType);
                services.AddScoped(interfacePaginatedCommandType, concreteGetFilterCommandHandlerType);

            }

            return builder;
        }
    }
}
