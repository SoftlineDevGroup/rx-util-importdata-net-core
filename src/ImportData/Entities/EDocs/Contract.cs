using System;
using System.Collections.Generic;
using System.Globalization;
using NLog;
using ImportData.IntegrationServicesClient.Models;
using System.IO;
using DocumentFormat.OpenXml.Drawing.Charts;
using ImportData.Entities.Databooks;
using System.Linq;

namespace ImportData
{
    public class Contract : Entity
    {
        public int PropertiesCount = 25;
        /// <summary>
        /// Получить наименование число запрашиваемых параметров.
        /// </summary>
        /// <returns>Число запрашиваемых параметров.</returns>
        public override int GetPropertiesCount()
        {
            return PropertiesCount;
        }

        /// <summary>
        /// Сохранение сущности в RX.
        /// </summary>
        /// <param name="shift">Сдвиг по горизонтали в XLSX документе. Необходим для обработки документов, составленных из элементов разных сущностей.</param>
        /// <param name="logger">Логировщик.</param>
        /// <returns>Число запрашиваемых параметров.</returns>
        public override IEnumerable<Structures.ExceptionsStruct> SaveToRX(Logger logger, bool supplementEntity, string ignoreDuplicates, int shift = 0)
        {
            var exceptionList = new List<Structures.ExceptionsStruct>();
            var variableForParameters = this.Parameters[shift + 0].Trim();

            // 1 номер договора
            var regNumber = this.Parameters[shift + 0].Trim();

            // 2 дата договора
            DateTimeOffset regDate = DateTimeOffset.MinValue;
            variableForParameters = this.Parameters[shift + 1].Trim();
            var style = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;
            var culture = CultureInfo.CreateSpecificCulture("en-GB");
            try
            {
                regDate = ParseDate(variableForParameters, style, culture);
                logger.Debug($"Дата договора {regDate}");
            }
            catch (Exception)
            {
                var message = string.Format("Не удалось обработать дату регистрации \"{0}\".", variableForParameters);
                exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Error, Message = message });
                logger.Error(message);

                return exceptionList;
            }

            // 3 Контрагент
            variableForParameters = this.Parameters[shift + 2].Trim();
            var counterparty = BusinessLogic.GetEntityWithFilter<ICounterparties>(c => c.Name == variableForParameters, exceptionList, logger);
            if (counterparty == null)
            {
                var message = string.Format("Не найден контрагент \"{0}\".", variableForParameters);
                exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Error, Message = message });
                logger.Error(message);

                return exceptionList;
            }
            logger.Debug($"Контрагент {counterparty?.Id}");

            // 4 Вид документа
            variableForParameters = this.Parameters[shift + 3].Trim();
            var documentKind = BusinessLogic.GetEntityWithFilter<IDocumentKinds>(d => d.Name == variableForParameters, exceptionList, logger);
            if (documentKind == null)
            {
                var message = string.Format("Не найден вид документа \"{0}\".", variableForParameters);
                exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Error, Message = message });
                logger.Error(message);

                return exceptionList;
            }
            logger.Debug($"Вид документа {documentKind?.Id}");

            // 5 Содержание
            var subject = this.Parameters[shift + 4];

            // 6 НОР
            variableForParameters = this.Parameters[shift + 5].Trim();
            var businessUnit = BusinessLogic.GetEntityWithFilter<IBusinessUnits>(b => b.Name == variableForParameters, exceptionList, logger);
            if (businessUnit == null)
            {
                var message = string.Format("Не найдена НОР \"{0}\".", variableForParameters);
                exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Error, Message = message });
                logger.Error(message);

                return exceptionList;
            }
            logger.Debug($"НОР {businessUnit?.Id}");

            // 7 Подразделение
            variableForParameters = this.Parameters[shift + 6].Trim();
            IDepartments department = null;
            if (businessUnit != null)
                department = BusinessLogic.GetEntityWithFilter<IDepartments>(d => d.Name == variableForParameters &&
                (d.BusinessUnit.Id == businessUnit.Id || d.BusinessUnit == null), exceptionList, logger, true);
            else
                department = BusinessLogic.GetEntityWithFilter<IDepartments>(d => d.Name == variableForParameters, exceptionList, logger);

            if (department == null)
            {
                var message = string.Format("Не найдено подразделение \"{0}\".", variableForParameters);
                exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Error, Message = message });
                logger.Error(message);

                return exceptionList;
            }
            logger.Debug($"Подразделение {department?.Id}");

            // 8 Подразделение ЕРП
            variableForParameters = this.Parameters[shift + 7].Trim();
            ICustomDepartments departmentErp = BusinessLogic.GetEntityWithFilter<ICustomDepartments>(d => d.Name == variableForParameters, exceptionList, logger);
            logger.Debug($"Подразделение ЕРП {departmentErp?.Id}");

            // 9 Путь к файлу
            var filePath = this.Parameters[shift + 8];
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

            // 10 Действует с
            DateTimeOffset validFrom = DateTimeOffset.MinValue;
            variableForParameters = this.Parameters[shift + 9].Trim();
            try
            {
                validFrom = ParseDate(variableForParameters, style, culture);
                logger.Debug($"Действует с {validFrom}");
            }
            catch (Exception)
            {
                /*var message = string.Format("Не удалось обработать значение в поле \"Действует с\" \"{0}\".", variableForParameters);
                exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Error, Message = message });
                logger.Error(message);

                return exceptionList;*/
            }

            // 11 действует по
            DateTimeOffset validTill = DateTimeOffset.MinValue;
            variableForParameters = this.Parameters[shift + 10].Trim();
            try
            {
                validTill = ParseDate(variableForParameters, style, culture);
                logger.Debug($"Действует по {validTill}");
            }
            catch (Exception)
            {
                /*var message = string.Format("Не удалось обработать значение в поле \"Действует по\" \"{0}\".", variableForParameters);
                exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Error, Message = message });
                logger.Error(message);

                return exceptionList;*/
            }

            // 12 сумма
            var totalAmount = 0.0;
            variableForParameters = this.Parameters[shift + 11].Trim();
            _ = double.TryParse(variableForParameters, style, culture, out totalAmount);
            logger.Debug($"Сумма {totalAmount}");

            // 13 Валюта
            variableForParameters = this.Parameters[shift + 12].Trim();
            var currency = BusinessLogic.GetEntityWithFilter<ICurrencies>(c => c.Name == variableForParameters, exceptionList, logger);
            logger.Debug($"Валюта {currency?.Id}");

            // 14 Валюта платежа
            variableForParameters = this.Parameters[shift + 13].Trim();
            var currencyPaid = BusinessLogic.GetEntityWithFilter<ICurrencies>(c => c.Name == variableForParameters, exceptionList, logger);
            logger.Debug($"Валюта платежа {currencyPaid?.Id}");

            // 15 Ставка НДС
            variableForParameters = this.Parameters[shift + 14].Trim();
            var vatRate = BusinessLogic.GetEntityWithFilter<IVatRates>(c => c.Name == variableForParameters, exceptionList, logger);
            logger.Debug($"Ставка НДС {vatRate?.Id}");

            // 16 Cумма НДС
            var totalAmountVat = 0.0;
            variableForParameters = this.Parameters[shift + 15].Trim();
            _ = double.TryParse(variableForParameters, style, culture, out totalAmountVat);
            logger.Debug($"Сумма НДС {totalAmountVat}");

            // 17 Детализация расчетов
            var details = this.Parameters[shift + 16].Trim();

            // 18 Статья ДДС
            variableForParameters = this.Parameters[shift + 17].Trim();
            var dds = BusinessLogic.GetEntityWithFilter<ITatSpirtPromDdsArticles>(c => c.Name == variableForParameters && c.IsGroup == false, exceptionList, logger);
            logger.Debug($"Статья ДДС {dds?.Id}");

            // 19 Состояние
            variableForParameters = this.Parameters[shift + 18].Trim();
            var lifeCycleState = BusinessLogic.GetPropertyLifeCycleState(variableForParameters);
            logger.Debug($"Состояние {lifeCycleState}");

            // 20 Ответственный
            variableForParameters = this.Parameters[shift + 19].Trim();
            var responsibleEmployee = BusinessLogic.GetEntityWithFilter<IEmployees>(e => e.Name == variableForParameters && e.EmploymentType == "MainPlace", exceptionList, logger);
            if (responsibleEmployee == null)
            {
                responsibleEmployee = BusinessLogic.GetEntityWithFilter<IEmployees>(e => e.Name == variableForParameters && e.Status == "Active", exceptionList, logger);
                if (responsibleEmployee == null)
                {
                    responsibleEmployee = BusinessLogic.GetEntityWithFilter<IEmployees>(e => e.Name == variableForParameters, exceptionList, logger);
                }
            }
            logger.Debug($"Ответственный {responsibleEmployee?.Id}");

            // 21 Подписал
            variableForParameters = this.Parameters[shift + 20].Trim();
            var ourSignatory = new IEmployees();
            ourSignatory = null;
            if (!string.IsNullOrWhiteSpace(variableForParameters))
            {
                ourSignatory = BusinessLogic.GetEntityWithFilter<IEmployees>(e => e.Name == variableForParameters, exceptionList, logger);
                logger.Debug($"Подписал {ourSignatory?.Id}");
            }

            // 22 Примечание
            var note = this.Parameters[shift + 21];

            // 23 Журнал регистрации
            var documentRegisterIdStr = this.Parameters[shift + 22].Trim();
            if (!int.TryParse(documentRegisterIdStr, out var documentRegisterId))
                if (ExtraParameters.ContainsKey("doc_register_id"))
                    int.TryParse(ExtraParameters["doc_register_id"], out documentRegisterId);

            var documentRegisters = documentRegisterId != 0 ? BusinessLogic.GetEntityWithFilter<IDocumentRegisters>(r => r.Id == documentRegisterId, exceptionList, logger) : null;
            if (documentRegisters == null)
            {
                var message = string.Format("Не найден журнал регистрации по ИД \"{0}\"", documentRegisterIdStr);
                exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Error, Message = message });
                logger.Error(message);

                return exceptionList;
            }

            // 24 Состояние регистрации
            var regState = this.Parameters[shift + 23].Trim();

            // 25 Внешний ИД
            var extId = this.Parameters[shift + 24].Trim();

            try
            {
                var isNewContract = false;
                var contracts = BusinessLogic.GetEntitiesWithFilter<IContracts>(x => x.RegistrationNumber != null &&
                    x.RegistrationNumber == regNumber &&
                    x.RegistrationDate.Value.ToString("d") == regDate.ToString("d") &&
                    x.Counterparty.Id == counterparty.Id &&
                    x.DocumentRegister.Id == documentRegisters.Id, exceptionList, logger, true);

                var contract = (IContracts)IOfficialDocuments.GetDocumentByRegistrationDate(contracts, regDate, logger, exceptionList);
                IContracts createdContract;
                if (contract == null)
                {
                    contract = new IContracts();
                    isNewContract = true;


                    // Обязательные поля.
                    contract.Name = fileNameWithoutExtension;
                    contract.Created = DateTimeOffset.UtcNow;
                    contract.Counterparty = counterparty;
                    contract.DocumentKind = documentKind;
                    contract.Subject = subject;
                    contract.BusinessUnit = businessUnit;
                    contract.Department = department;
                    if (validFrom != DateTimeOffset.MinValue)
                        contract.ValidFrom = validFrom;
                    else
                        contract.ValidFrom = null;
                    if (validTill != DateTimeOffset.MinValue)
                        contract.ValidTill = validTill;
                    else
                        contract.ValidTill = null;
                    contract.TotalAmount = totalAmount;
                    contract.Currency = currency;

                    contract.PaymentCurrencysline = currencyPaid;
                    contract.DdsArticlesline = dds;
                    contract.VATRatesline = vatRate;
                    contract.VATAmountsline = totalAmount;
                    contract.ERPDepartmentsline = departmentErp;

                    contract.ResponsibleEmployee = responsibleEmployee;
                    contract.OurSignatory = ourSignatory;
                    contract.Note = note;
                    contract.ExternalIdsline = extId;

                    contract.DocumentRegister = documentRegisters;
                    if (regDate != DateTimeOffset.MinValue)
                        contract.RegistrationDate = regDate.UtcDateTime;
                    else
                        contract.RegistrationDate = null;
                    contract.RegistrationNumber = regNumber;
                    if (!string.IsNullOrEmpty(contract.RegistrationNumber) && contract.DocumentRegister != null)
                        contract.RegistrationState = BusinessLogic.GetRegistrationsState(regState);

                    createdContract = BusinessLogic.CreateEntity(contract, exceptionList, logger);
                    logger.Debug($"Договор {createdContract?.Id}");
                    if (createdContract != null)
                    {
                        createdContract.UpdateLifeCycleState(lifeCycleState);

                        BusinessLogic.ImportBody(createdContract, filePath, logger, true);
                    }
                }
            }
            catch (Exception ex)
            {
                exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Error, Message = ex.Message });
                logger.Error(ex, ex.Message, ex.StackTrace);
                return exceptionList;
            }

            return exceptionList;
        }
    }
}
