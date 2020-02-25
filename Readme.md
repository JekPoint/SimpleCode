# Описание
Проект содержит API для доступа к данным каталога вин.

## API
Используется GraphQL для работы с данными.
Клиентская часть использует библиотеку Apollo для выполнения запросов.


## Общие принципы
 
Основная логика работы с графом, реализована в классах *Builder.cs в папках SmartAnalytics.HonestWine.PIM/Type. Данные классы реализуют базовый CRUD.
Данную логику можно изменять или отключать, например:
```
    using System;
    using Microsoft.Extensions.Logging;
    using SmartAnalytics.HonestWine.PIM.Base;

    namespace SmartAnalytics.HonestWine.PIM.Type.Country 
    {
        public class CountryBuilder : BaseGraphQlBuilder<Data.Models.Country, CountryInputType, CountryType>
        {
            public CountryBuilder(IServiceProvider provider, ILogger<object> logger)
                : base(provider, logger)
            {}

            public override void Delete(string commandName)
            {
                Mutation.FieldAsync<IntGraphType>(commandName,
                arguments: new QueryArguments(new QueryArgument<NonNullGraphType<IntGraphType>> {Name = "id"}),
                resolve: async context =>
                {
                    using (var scope = Provider.CreateScope())
                    {
                        using (var connection = await scope.ServiceProvider.GetService<IConnectionFactory>().ConnectAsync())
                        {
                            var id = context.GetArgument<int>("id");
                            var query = SqlBuilder.Delete($@"hw.""{ModelName}""");
                            query.Parameters.Add("id", id);
                            return await query.ExecuteAsync(connection);
                        }
                    }
                });
            }

        }
    }
```
Сделав override метода Delete мы можем переопределить логику удаления записей из таблицы Country. В данном примере мы работаем с DAPPER, используя интерфейс IConnectionFactory,
но мы так же можем использовать и Etity, используя интерфейс IHonestWineRepository.

## Процедура добавления поля в одну из существующих таблиц
- Добавить в модель новое свойство (поле из таблицы), к примеру SmartAnalytics.HonestWine.PIM/Data/Models/Country.cs новое поле (public int test {get;set;}), 
указав при этом аттрибуты валидации  ([Required] если поле не nullable)
```
    public class Country : IBaseModel
    {
        [Required]
        public int TestNotNull {get;set;}

        public int? TestNull {get;set;}

        (все поля остальные поля, присутствующие в Таблице Country)
    }
```
- В папке SmartAnalytics.HonestWine.PIM/Type/Country в файле CountryInputType.cs добавить новое поле, если необходимо чтобы поле было доступно для обновления   
```
    public class CountryInputType : InputObjectGraphType<Data.Models.Country>
    {
        public CountryInputType()
        {
            Name = $"{nameof(Data.Models.Country)}Input";
            Field(x => x.TestNotNull, true);
            Field(x => x.TestNull, true);
            (все остальные поля указанные в CountryInputType.cs)
        }
    }
```
В данном случае запись Field(x => x.testNotNull, true); говорит о том, что можно добавить не обязательные к указанию поле с именем testNotNull, поля мутиции будут выглядить так
```
    mutation{
      addCountry(country:{
        "testNotNull": 1,
        "testNull": null,
        (все поля указанные в CountryInputType.cs)
      })
      {
        (все поля указанные в CountryType.cs)
      }
    }
```


- CountryType.cs указать новое поле из модели, если необходимо чтобы поле было доступно для запроса
```
public class CountryType : ObjectGraphType<Data.Models.Country>
    {
        public CountryType()
        {
            Name = $"{nameof(Data.Models.Country)}Output";

            Field(x => x.TestNotNull);
            Field(x => x.TestNull, true);
        }
    }
```
В данном случае запись Field(x => x.testNotNull, true); указывает, что можно добавить не обязательное к указанию поле с именем testNotNull, поля запроса будут выглядить так
```
    {
      contrys(country:
      {
        testNotNull
        testNull
        (все поля указанные в CountryType.cs)
      }
    }
```



## Процедура добавления новой таблицы
- Добавить модель таблицы в SmartAnalytics.HonestWine.PIM/Data/Models
- Добавить папку, название которой, соответствует модели, по пути SmartAnalytics.HonestWine.PIM/Type, например Country
- В созданной папке, создать 5 файлов, например для Country файлы должны называть следующим образом
```
    CountryBuilder.cs - файл содержащий основную логику обработки Query и Mutation
    CountryEntityMapper.cs - файл Dapper.GraphQL, необходимый для маппинга ответа из базы на модель таблиц
    CountryInputType.cs - входной тип графа, необходимый для добавления и обновления сущьности
    CountryQueryBuilder - файл содержащий логику создания запроса на основании модели и графа, пришедшего в запросе
    CountryType.cs - выходной тип графа. Необходим для получения данных из запросов добавления, обновления и запроса самих данных
```

## Пример добавления файла *Builder.cs
```
    using System;
    using Microsoft.Extensions.Logging;
    using SmartAnalytics.HonestWine.PIM.Base;

    namespace SmartAnalytics.HonestWine.PIM.Type.Country 
    {
        public class CountryBuilder : BaseGraphQlBuilder<Data.Models.Country, CountryInputType, CountryType>
        {
            public CountryBuilder(IServiceProvider provider, ILogger<object> logger)
                : base(provider, logger)
            {
            }
        }
    }    
```
BaseGraphQlBuilder - базовый класс реализующий CRUD операции в графе. После наследования буду созданы следующие графы

Query на запрос данных
```
{
  countrys{
    (все поля указанные в CountryType.cs)  
  }
}
```

Mutation (addCountry) - добавление в таблицу Country данных
```
mutation{
  addCountry(country:{
    (все поля указанные в CountryInputType.cs)
  })
  {
    (все поля указанные в CountryType.cs)
  }
}
```

Mutation (updateCountry) - обновление данных в таблице Country
```
mutation{
  updateCountry(country:{
    (все поля указанные в CountryInputType.cs)
  })
  {
    (все поля указанные в CountryType.cs)
  }
}
```

Mutation (deleteCountry) - удаление записи из таблицы Country
```
mutation{
  deleteCountry(id: countryId)
}
```

Если необходимо отключить или изменить какой - либо функционал, то в классе *Builder.cs делается override тех полей, которые отвечают за тот или иной функционал в базавом классе
```
    using System;
    using Microsoft.Extensions.Logging;
    using SmartAnalytics.HonestWine.PIM.Base;

    namespace SmartAnalytics.HonestWine.PIM.Type.Country 
    {
        public class CountryBuilder : BaseGraphQlBuilder<Data.Models.Country, CountryInputType, CountryType>
        {
            public CountryBuilder(IServiceProvider provider, ILogger<object> logger)
                : base(provider, logger)
            {}

            public override void Insert(string commandName)
            {}

            public override void Delete(string commandName)
            {}

            public override void Update(string commandName)
            {}

            public override void GetList(string commandName)
            {}
        }
    }
```

## Пример добавления файла *EntityMapper.cs
```
    using Dapper.GraphQL;
    using Dapper.GraphQL.Contexts;
    using Dapper.GraphQL.Extensions;
    using SmartAnalytics.HonestWine.PIM.Type.CountryLocale;

    namespace SmartAnalytics.HonestWine.PIM.Type.Country
    {
        public class CountryEntityMapper : DeduplicatingEntityMapper<Data.Models.Country>
        {
            private readonly CountryLocaleEntityMapper _countryLocale;

            public CountryEntityMapper()
            {
                PrimaryKey = key => key.Id;

                _countryLocale = new CountryLocaleEntityMapper();
            }

            public override Data.Models.Country Map(EntityMapContext context)
            {
                var model = context.Start<Data.Models.Country>();
                if (model == null)
                {
                    return null;
                }

                var locale = context.Next("countryLocale", _countryLocale);
                if (locale != null && model.CountryLocale != locale)
                {
                    model.CountryLocale = locale;
                }

                return model;
            }

            public override void ClearAnyCache()
            {
                _countryLocale.ClearAnyCache();
                KeyCache.Clear();
            }
        }
    }
```
В конструкторе объявляются мапперы связанных таблиц и указывается ключевое поле, в нашем случае это всегда Id.
Далее делается  override методу Map, который возвращает результирующую модель ответа. В этом методе указываются соответствие
 полей из запроса графа, и полей модели, связанных таблиц, в которые необходимо положить результат.

## Пример добавления *InputType.cs
```
using GraphQL.Types;
using SmartAnalytics.HonestWine.PIM.Type.EntityType;

namespace SmartAnalytics.HonestWine.PIM.Type.Country
{
    public class CountryInputType : InputObjectGraphType<Data.Models.Country>
    {
        public CountryInputType()
        {
            Name = $"{nameof(Data.Models.Country)}Input";

            Field(x => x.Id, true);
            Field(x => x.CountryRank, true);
            Field(x => x.CountryCode, true);
            Field(x => x.JurisdictionOfFormation, true);
            Field(x => x.CountrySubDivisionCode, true);
            Field(x => x.CountryWineProduction, true);
            Field(x => x.CountryFlagIcon, true);
            Field<EntityTypeInputType>("entityType");
        }
    }
}
```
В большинстве случаев все поля указанные в InputType.cs соответствуют полям модели. Этот класс необходим для формирования входной 
модели для записи или обновления данных

## пример файла *QueryBuilder.cs
```
    using System;
    using SmartAnalytics.HonestWine.PIM.Base;

    namespace SmartAnalytics.HonestWine.PIM.Type.Country
    {
        public class CountryQueryBuilder : BaseQueryBuilder<Data.Models.Country>
        {
            public CountryQueryBuilder(IServiceProvider provider)
                : base(provider)
            {
            }
        }
    }
```
Данный файл позволяет на основании модели сформировать SQL запросы для выборки данных. 

## пример файла *Type.cs
```
    using GraphQL.Types;
    using SmartAnalytics.HonestWine.PIM.Type.CountryLocale;
    using SmartAnalytics.HonestWine.PIM.Type.EntityType;

    namespace SmartAnalytics.HonestWine.PIM.Type.Country
    {
        public class CountryType : ObjectGraphType<Data.Models.Country>
        {
            public CountryType()
            {
                Name = $"{nameof(Data.Models.Country)}Output";

                Field(x => x.Id);
                Field(x => x.CountryRank, true);
                Field(x => x.CountryWineProduction, true);
                Field(x => x.CountryCode, true);
                Field(x => x.JurisdictionOfFormation, true);
                Field(x => x.CountrySubDivisionCode, true);
                Field(x => x.CountryFlagIcon, true);

                Field<CountryLocaleType>("countryLocale",
                    arguments: new QueryArguments(new QueryArgument<IntGraphType> {Name = "localeId"}),
                    resolve: context => context.Source?.CountryLocale);

                Field<EntityTypeType>("entityType", resolve: context => context.Source?.EntityType);
            }
        }
    }
```
Данный файл описывает модель данных, которая возвращается клиенту

## Playground
/ui/playground - playground для тестирования GraphQL запросов.
Недоступен в production и staging (скрывается).

### Работа с playground

Для работы с playground необходимо:
1. Залогиниться в рабочей версии системы для получения JWT токена и идентификатора профиля. Можно найти в заголовках любого ajax заспрос - хедеры Authorization и ProfileId. Скопировать значения.  
2. Зайти в playground /ui/playground/, в настройках слева снизу добавить значения http headers:
```
{
    "Authorization": "Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IjM5YjgzYjY5ZTJhMTg1NDk0OTA0ODQ2MGJjMDk0YTYwIiwidHlwIjoiSldUIn0.eyJuYmYiOjE1NzQzMjQ4MDUsImV4cCI6MTU3NDQxMTIwNSwiaXNzIjoiaHR0cHM6Ly9ody5hcHAudGVzdC9pZGVudGl0eSIsImF1ZCI6WyJodHRwczovL2h3LmFwcC50ZXN0L2lkZW50aXR5L3Jlc291cmNlcyIsInd3bCJdLCJjbGllbnRfaWQiOiJ3d2wiLCJzdWIiOiIzMzYiLCJhdXRoX3RpbWUiOjE1NzQzMjQ4MDUsImlkcCI6ImxvY2FsIiwicm9sZSI6Ik5hdHVyYWxQZXJzb24iLCJ1c2VyX2lkIjoiMzM2IiwiZW1haWwiOiJhbmRyZXkubGF6YXJldkBzbS1hbmFseXRpY3MuY29tIiwic2NvcGUiOlsib3BlbmlkIiwid3dsIl0sImFtciI6WyJwd2QiXX0.m8-r7GfU7ZhmRhJ_2XhIj2dHK9LVPXd0GVA25bpSt8s0NU05e7wHly5v7CC8zoD3sezWdcGaGZzEoxALpZGVO2bX7Bz8dDRK7Dn7L5ZUXlDDa4ia9PQJhlT17x_Wf_zqgGYIswahxSVYQi6r5K9BZYsdj2vfQYHzK6z5KAd6qkoJ976RxfaQmuEbxPk-jXlsjNrtKoC05b0Ap-WvsUQOjaqUd1SKX6UuajzoO1YMbFaw1oO2e7ZFb-IS2qB-spLNwBAxWKIbCL8tqJrLFwEBW5IXcvweafIYX6xmGOcgrkg1IU21uL5_mDzpewkdMKc0RLJvwBJULXAQ1Z1BMjCLwg",
    "ProfileId": 443
}
```
3. При использовании некоторых параметризованных GraphQL запросов нужно будет также прописать значения параметров в соседней вкладке Query Variables.  
4. Ссылка на endpoing во вкладке playground должна быть вида \<domain\>/api/v1/wwl  
5. После выполнения вышеперечисленных шагов playground готов к работе.  