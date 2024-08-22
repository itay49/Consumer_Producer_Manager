using System;
using Microsoft.Extensions.Logging;

namespace Renassaince.QueueManager
{
    public class ProducerFactory
    {
        private static JsonParser jsonParser = new JsonParser();

        /// <summary>
        /// DataAccessEnum- for which kind of Producer to use
        /// loginData-string that is json, for the params needed to init the Producer
        /// name- needed for situation we need 2 or more kinds of Producer but use singelton in DI
        /// log- for write the log in the same project log spot
        /// </summary>
        public static IProducer CreateProducer(DataAccessEnum dataAccess,
            string loginData, string name, ILogger log)
        {
            try
            {
                log.LogInformation("[ProducerFactory:CreateProducer]: Start CreateProducer method");
                switch (dataAccess)
                {
                    case DataAccessEnum.EMS:
                        DetailsConnectEMS detailsEMS =
                            jsonParser.StringToObject<DetailsConnectEMS>(loginData);
                        EmsSender emsSender = new EmsSender(detailsEMS, 
                            Guid.NewGuid().ToString() + "_To", name, log);
                        log.LogInformation("[ProducerFactory:CreateProducer]: Finish CreateProducer method. return EmsSender");
                        return emsSender;
                    case DataAccessEnum.File:
                        DetailsConnectFile detailsFile =
                            jsonParser.StringToObject<DetailsConnectFile>(loginData);
                        FileSender fileSender = new FileSender(detailsFile, name, log);
                        log.LogInformation("[ProducerFactory:CreateProducer]: Finish CreateProducer method. return FileSender");
                        return fileSender;
                    case DataAccessEnum.RABBIT:
                        DetailsConnectRabbit detailsRabbit =
                            jsonParser.StringToObject<DetailsConnectRabbit>(loginData);
                        RabbitSender rabbitSender = new RabbitSender(detailsRabbit, name, log);
                        log.LogInformation("[ProducerFactory:CreateProducer]: Finish CreateProducer method. return RabbitSender");
                        return rabbitSender;
                    case DataAccessEnum.REST:
                        DetailsConnectRest detailsSenderRest =
                            jsonParser.StringToObject<DetailsConnectRest>(loginData);
                        RestSender restSender= new RestSender(detailsSenderRest, name, log);
                        log.LogInformation("[ProducerFactory:CreateProducer]: Finish CreateProducer method. return RestSender");
                        return restSender;
                    case DataAccessEnum.AMQ:
                        DetailsConnectActiveMq detailsConnectActiveMq =
                            jsonParser.StringToObject<DetailsConnectActiveMq>(loginData);
                        ActiveMqSender activeMqSender = new ActiveMqSender(detailsConnectActiveMq, name, log);
                        log.LogInformation("[ProducerFactory:CreateProducer]: Finish CreateProducer method. return RestSender");
                        return activeMqSender;
                    default:
                        throw new DataAccessNotFoundException
                            ("[ProducerFactory:CreateProducer]- this kind of DataAccess(Producer) is not implemented at the moment");
                }
            }
            catch (ParserException ex)
            {
                log.LogError($"[ProducerFactory:CreateProducer]: The Producer is: {dataAccess}. ParserException:. {ex}");
                throw;
            }

            catch (DataAccessNotFoundException ex)
            {
                log.LogError($"[ProducerFactory:CreateProducer]: The Producer is: {dataAccess}. DataAccessNotFoundException:. {ex}");
                throw;
            }

            catch (Exception ex)
            {
                log.LogError($"[ProducerFactory:CreateProducer]: The Producer is: {dataAccess}. {ex}");
                throw;
            }
        }


        /// <summary>
        /// DataAccessEnum- for which kind of Producer to use
        /// loginData-string that is json, for the params needed to init the Producer
        /// name- needed for situation we need 2 or more kinds of Producer but use singelton in DI
        /// log- for write the log in the same project log spot
        /// </summary>
        public static IProducer CreateSingeltonProducer(DataAccessEnum dataAccess,
            string loginData, string name, ILogger log)
        {
            try
            {
                switch (dataAccess)
                {
                    case DataAccessEnum.EMS:
                        DetailsConnectEMS detailsEMS =
                            jsonParser.StringToObject<DetailsConnectEMS>(loginData);
                        EmsSingeltonSender emsSender = EmsSingeltonSender.GetInstance(detailsEMS,
                            Guid.NewGuid().ToString() + "_To", name, log);
                        log.LogInformation("[ProducerFactory:CreateProducer]: Finish CreateProducer method. return EmsSender");
                        return emsSender;
                    case DataAccessEnum.File:
                        DetailsConnectFile detailsFile =
                            jsonParser.StringToObject<DetailsConnectFile>(loginData);
                        FileSingeltonSender fileSender = FileSingeltonSender.GetInstance(detailsFile, name, log);
                        log.LogInformation("[ProducerFactory:CreateProducer]: Finish CreateProducer method. return FileSender");
                        return fileSender;
                    case DataAccessEnum.RABBIT:
                        DetailsConnectRabbit detailsRabbit =
                            jsonParser.StringToObject<DetailsConnectRabbit>(loginData);
                        RabbitSingeltonSender rabbitSender = RabbitSingeltonSender.GetInstance(detailsRabbit, name, log);
                        log.LogInformation("[ProducerFactory:CreateProducer]: Finish CreateProducer method. return RabbitSender");
                        return rabbitSender;
                    //throw new DataAccessNotFoundException
                    //    ("[ProducerFactory:CreateProducer]- this kind of DataAccess(Producer) is not implemented at the moment");
                    default:
                        throw new DataAccessNotFoundException
                            ("[ProducerFactory:CreateSingeltonProducer]- this kind of DataAccess(Producer) is not implemented at the moment");
                }
            }
            catch (ParserException ex)
            {
                log.LogError($"[ProducerFactory:CreateSingeltonProducer]: The Producer is: {dataAccess}. ParserException:. {ex}");
                throw;
            }

            catch (DataAccessNotFoundException ex)
            {
                log.LogError($"[ProducerFactory:CreateSingeltonProducer]: The Producer is: {dataAccess}. DataAccessNotFoundException:. {ex}");
                throw;
            }

            catch (Exception ex)
            {
                log.LogError($"[ProducerFactory:CreateSingeltonProducer]: The Producer is: {dataAccess}. {ex}");
                throw;
            }
        }
    }
}
