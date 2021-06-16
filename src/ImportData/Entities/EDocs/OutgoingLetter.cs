﻿using System;
using System.Collections.Generic;
using System.Globalization;
using NLog;
using ImportData.IntegrationServicesClient.Models;
using System.IO;

namespace ImportData
{
    class OutgoingLetter : Entity
    {
        public int PropertiesCount = 15;
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
            var regNumber = this.Parameters[shift + 0];
            DateTimeOffset regDate = DateTimeOffset.MinValue;
            var style = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;
            var culture = CultureInfo.CreateSpecificCulture("en-GB");
            var regDateDouble = 0.0;

            if (!string.IsNullOrWhiteSpace(this.Parameters[shift + 1]) && !double.TryParse(this.Parameters[shift + 1].Trim(), style, culture, out regDateDouble))
            {
                var message = string.Format("Не удалось обработать дату регистрации \"{0}\".", this.Parameters[shift + 1]);
                exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Error, Message = message });
                logger.Error(message);

                return exceptionList;
            }
            else
            {
                if (!string.IsNullOrEmpty(this.Parameters[shift + 1].ToString()))
                    regDate = DateTime.FromOADate(regDateDouble);
            }

            variableForParameters = this.Parameters[shift + 2].Trim();
            var counterparty = BusinessLogic.GetEntityWithFilter<ICounterparties>(c => c.Name == variableForParameters, exceptionList, logger);

            if (counterparty == null)
            {
                var message = string.Format("Не найден контрагент \"{0}\".", this.Parameters[shift + 2]);
                exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Error, Message = message });
                logger.Error(message);

                return exceptionList;
            }

            variableForParameters = this.Parameters[shift + 3].Trim();
            var documentKind = BusinessLogic.GetEntityWithFilter<IDocumentKinds>(d => d.Name == variableForParameters, exceptionList, logger);

            if (documentKind == null)
            {
                var message = string.Format("Не найден вид документа \"{0}\".", this.Parameters[shift + 3]);
                exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Error, Message = message });
                logger.Error(message);

                return exceptionList;
            }

            var subject = this.Parameters[shift + 4];

            variableForParameters = this.Parameters[shift + 5].Trim();
            var department = BusinessLogic.GetEntityWithFilter<IDepartments>(d => d.Name == variableForParameters, exceptionList, logger);

            if (department == null)
            {
                var message = string.Format("Не найдено подразделение \"{0}\".", this.Parameters[shift + 5]);
                exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Error, Message = message });
                logger.Error(message);

                return exceptionList;
            }

            variableForParameters = this.Parameters[shift + 6].Trim();
            var preparedBy = BusinessLogic.GetEntityWithFilter<IEmployees>(e => e.Name == variableForParameters, exceptionList, logger);

            if (!string.IsNullOrEmpty(this.Parameters[shift + 6].Trim()) && preparedBy == null)
            {
                var message = string.Format("Не найден Подготавливающий \"{2}\". Исходящее письмо: \"{0} {1}\". ", regNumber, regDate.ToString(), this.Parameters[shift + 6].Trim());
                exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Warn, Message = message });
                logger.Warn(message);
            }

            var filePath = this.Parameters[shift + 7];
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

            var note = this.Parameters[shift + 8];

            variableForParameters = this.Parameters[shift + 9].Trim();
            var deliveryMethod = BusinessLogic.GetEntityWithFilter<IMailDeliveryMethods>(m => m.Name == variableForParameters, exceptionList, logger);

            if (!string.IsNullOrEmpty(this.Parameters[shift + 10].Trim()) && deliveryMethod == null)
            {
                var message = string.Format("Не найден Способ доставки \"{2}\". Входящее письмо: \"{0} {1}\". ", regNumber, regDate.ToString(), this.Parameters[shift + 9].Trim());
                exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Warn, Message = message });
                logger.Warn(message);
            }

            variableForParameters = this.Parameters[shift + 10].Trim();
            int idDocumentRegisters = int.Parse(variableForParameters);
            var documentRegisters = BusinessLogic.GetEntityWithFilter<IDocumentRegisters>(r => r.Id == idDocumentRegisters, exceptionList, logger);

            if (documentRegisters == null)
            {
                var message = string.Format("Не найден Журнал регистрации по ИД: \"{3}\". Входящее письмо: \"{0} {1} {2}\". ", regNumber, regDate.ToString(), counterparty, this.Parameters[shift + 10].Trim());
                exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Warn, Message = message });
                logger.Warn(message);

                return exceptionList;
            }

            try
            {
                if (ignoreDuplicates.ToLower() != Constants.ignoreDuplicates.ToLower())
                {
                    var outgoingLetters = BusinessLogic.GetEntityWithFilter<IOutgoingLetters>(x => x.RegistrationNumber == regNumber && x.RegistrationDate.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'") == regDate.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'"), exceptionList, logger);

                    // Обновление сущности при условии, что найдено одно совпадение.
                    if (outgoingLetters != null)
                    {
                        outgoingLetters.Name = fileNameWithoutExtension;
                        outgoingLetters.DocumentRegister = documentRegisters;
                        outgoingLetters.Correspondent = counterparty;

                        outgoingLetters.Created = DateTimeOffset.UtcNow;
                        outgoingLetters.RegistrationDate = regDate != DateTimeOffset.MinValue ? regDate : Constants.defaultDateTime;

                        outgoingLetters.RegistrationNumber = regNumber;
                        outgoingLetters.DocumentKind = documentKind;
                        outgoingLetters.Subject = subject;
                        outgoingLetters.Department = department;

                        if (department != null)
                            outgoingLetters.BusinessUnit = department.BusinessUnit;

                        outgoingLetters.PreparedBy = preparedBy;
                        outgoingLetters.Note = note;

                        var updatedEntity = BusinessLogic.UpdateEntity<IOutgoingLetters>(outgoingLetters, exceptionList, logger);

                        return exceptionList;
                    }
                }

                var outgoingLetter = new IOutgoingLetters();

                outgoingLetter.Name = fileNameWithoutExtension;
                outgoingLetter.DocumentRegister = documentRegisters;
                outgoingLetter.Correspondent = counterparty;

                outgoingLetter.Created = DateTimeOffset.UtcNow;
                outgoingLetter.RegistrationDate = regDate != DateTimeOffset.MinValue ? regDate : Constants.defaultDateTime;

                outgoingLetter.RegistrationNumber = regNumber;
                outgoingLetter.DocumentKind = documentKind;
                outgoingLetter.Subject = subject;
                outgoingLetter.Department = department;

                if (department != null)
                    outgoingLetter.BusinessUnit = department.BusinessUnit;

                outgoingLetter.PreparedBy = preparedBy;
                outgoingLetter.Note = note;

                var createdOutgoingLetter = BusinessLogic.CreateEntity<IOutgoingLetters>(outgoingLetter, exceptionList, logger);

                if (!string.IsNullOrWhiteSpace(filePath))
                    exceptionList.AddRange(BusinessLogic.ImportBody(createdOutgoingLetter, filePath, logger));

                var documentRegisterId = 0;

                if (ExtraParameters.ContainsKey("doc_register_id"))
                    if (int.TryParse(ExtraParameters["doc_register_id"], out documentRegisterId))
                        exceptionList.AddRange(BusinessLogic.RegisterDocument(outgoingLetter, documentRegisterId, regNumber, regDate, Constants.RolesGuides.RoleOutgoingDocumentsResponsible, logger));
                    else
                    {
                        var message = string.Format("Не удалось обработать параметр \"doc_register_id\". Полученное значение: {0}.", ExtraParameters["doc_register_id"]);
                        exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Error, Message = message });
                        logger.Error(message);

                        return exceptionList;
                    }
            }
            catch (Exception ex)
            {
                exceptionList.Add(new Structures.ExceptionsStruct { ErrorType = Constants.ErrorTypes.Error, Message = ex.Message });

                return exceptionList;
            }

            return exceptionList;
        }
    }
}
