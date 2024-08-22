using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Renassaince.QueueManager
{
    public class ConsumerFactory
    {
        private static JsonParser jsonParser = new JsonParser();

        /// <summary>
        /// DataAccessEnum- for which kind of Consumer to use
        /// loginData-string that is json, for the params needed to init the Consumer
        /// name- needed for situation we need 2 or more kinds of Consumer but use singelton in DI
        /// log- for write the log in the same project log spot
        /// </summary>
        public static IConsumer CreateConsumer(DataAccessEnum dataAccess,
            string loginData, string name, ILogger log)
        {
            try
            {
                log.LogInformation("[ConsumerFactory:CreateConsumer]: Start CreateConsumer method");
                switch (dataAccess)
                {
                    case DataAccessEnum.EMS:
                        DetailsConnectEMS detailsEMS =
                            jsonParser.StringToObject<DetailsConnectEMS>(loginData);
                        EmsListener emsListener= new EmsListener(detailsEMS, Guid.NewGuid().ToString() + "_From",
                            name, log);
                        log.LogInformation("[ConsumerFactory:CreateConsumer]: Finish CreateConsumer method. return EmsListener");
                        return emsListener;
                    case DataAccessEnum.File:
                        DetailsConnectFile detailsFile =
                            jsonParser.StringToObject<DetailsConnectFile>(loginData);
                        FileListener fileListener= new FileListener(detailsFile, name, log);
                        log.LogInformation("[ConsumerFactory:CreateConsumer]: Finish CreateConsumer method. return FileListener");
                        return fileListener;
                    case DataAccessEnum.RABBIT:
                        DetailsConnectRabbit detailsRabbit =
                            jsonParser.StringToObject<DetailsConnectRabbit>(loginData);
                        RabbitListener rabbitListener = new RabbitListener(detailsRabbit, name, log);
                        log.LogInformation("[ConsumerFactory:CreateConsumer]: Finish CreateConsumer method. return RabbitListener");
                        return rabbitListener;
                        //throw new DataAccessNotFoundException
                        //    ("[ConsumerFactory:CreateConsumer]- this kind of DataAccess(Consumer) is not implemented at the moment");
                    default:
                        throw new DataAccessNotFoundException
                            ("[ConsumerFactory:CreateConsumer]- this kind of DataAccess(Consumer) is not implemented at the moment");

                }
            }

            catch (ParserException ex)
            {
                log.LogError($"[ConsumerFactory:CreateConsumer]: The Consumer is: {dataAccess}. ParserException:{ex}");
                throw;

            }

            catch (DataAccessNotFoundException ex)
            {
                log.LogError($"[ConsumerFactory:CreateConsumer]: The Consumer is: {dataAccess}. DataAccessNotFoundException:{ex}");
                throw;
            }

            catch (Exception ex)
            {
                log.LogError($"[ConsumerFactory:CreateConsumer]: The Consumer is: {dataAccess}. Exception:{ex}");
                throw;
            }
        }

        /// <summary>
        /// DataAccessEnum- for which kind of Consumer to use
        /// loginData-string that is json, for the params needed to init the Consumer
        /// name- needed for situation we need 2 or more kinds of Consumer but use singelton in DI
        /// log- for write the log in the same project log spot
        /// </summary>
        public static IConsumer CreateSingeltonConsumer(DataAccessEnum dataAccess,
            string loginData, string name, ILogger log)
        {
            try
            {
                log.LogInformation("[ConsumerFactory:CreateSingeltonConsumer]: Start CreateConsumer method");
                switch (dataAccess)
                {
                    case DataAccessEnum.EMS:
                        DetailsConnectEMS detailsEMS =
                            jsonParser.StringToObject<DetailsConnectEMS>(loginData);
                        EmsSingeltonListener emsListener = EmsSingeltonListener.GetInstance(detailsEMS, 
                            Guid.NewGuid().ToString() + "_From", name, log);
                        log.LogInformation("[ConsumerFactory:CreateSingeltonConsumer]: Finish CreateConsumer method. return EmsListener");
                        return emsListener;
                    case DataAccessEnum.File:
                        DetailsConnectFile detailsFile =
                            jsonParser.StringToObject<DetailsConnectFile>(loginData);
                        FileSingeltonListener fileListener = FileSingeltonListener.GetInstance(detailsFile, name, log);
                        log.LogInformation("[ConsumerFactory:CreateSingeltonConsumer]: Finish CreateConsumer method. return FileListener");
                        return fileListener;
                    case DataAccessEnum.RABBIT:
                        DetailsConnectRabbit detailsRabbit =
                            jsonParser.StringToObject<DetailsConnectRabbit>(loginData);
                        RabbitSingeltonListener rabbitListener = RabbitSingeltonListener.GetInstance(detailsRabbit, name, log);
                        log.LogInformation("[ConsumerFactory:CreateSingeltonConsumer]: Finish CreateConsumer method. return RabbitListener");
                        return rabbitListener;
                    //throw new DataAccessNotFoundException
                    //    ("[ProducerFactory:CreateProducer]- this kind of DataAccess(Producer) is not implemented at the moment");
                    default:
                        throw new DataAccessNotFoundException
                            ("[ConsumerFactory:CreateSingeltonConsumer]- this kind of DataAccess(Listener) is not implemented at the moment");

                }
            }
            catch (ParserException ex)
            {
                log.LogError($"[ConsumerFactory:CreateSingeltonConsumer]: The Consumer is: {dataAccess}. ParserException:{ex}");
                throw;
            }

            catch (DataAccessNotFoundException ex)
            {
                log.LogError($"[ConsumerFactory:CreateSingeltonConsumer]: The Consumer is: {dataAccess}. DataAccessNotFoundException:{ex}");
                throw;
            }

            catch (Exception ex)
            {
                log.LogError($"[ConsumerFactory:CreateSingeltonConsumer]: The Consumer is: {dataAccess}. Exception:{ex}");
                throw;
            }
        }
    }
}
