# Выбор и настройка мониторинга в системе

## Мотивация

На данный момент в компании "Александрит" практически отсутствует мониторинг систем. Это приводит к проблемам:
- переполнение очередей заказов не видны.
- нет возможности выявить узкие места - нет понимания какие именно места системы требуется улучшать.
- проблемы конкретных заказов проявляются только после жалоб клиентов. 

Внедрение системы мониторинга позводит:
1. Собирать показатели SLA, time-to-market, и принимать меры по их улучшению, а так же прогнозировать реакцию текущих мощностей на повышение нагрузки.
2. Выявлять слабые места системы, принимать меры по их устранению.
3. Контролировать производительность систем, как вцелом, так и по отдельности.
4. Устранять проблемы по конкретным заказам при первом их появлении, а не после жалоб от клиентов.

## Выбор подхода к мониторингу

Для реализации мониторинга следует использовать проверенные подходы:

1. USE (утилизация, насыщенность, ошибки) - для инфраструктуры и базовых метрик приложений. Следует использовать для EC2-хостов, RabbitMQ-сервера, производительность MES и CRM приложений.
2. 4 золотых правила (задержка, трафик, ошибки, насыщенность) - для оценки загруженности клиентских интерфейсов (MES API и InternetShop).
3. RED (частота запросов, ошибки, длительность) - для мониоринга backend-систем.

## Выбор метрик

### Из существующих метрик, для оперативного решения проблем производительности, следует организовать сбор следующих:

| Метрика | Ярлыки(Labels) | 
|---|---|
| 1. Number of `dead-letter-exchange` letters in RabbitMQ  | queueName  | 
| 2. Number of message `in flight` in RabbitMQ  | queueName  | 
| 3. Number of requests (RPS) for internet shop API  | path, method | 
| 4. Number of requests (RPS) for CRM API  | path, method | 
| 5. Number of requests (RPS) for MES API  | path, method | 
| 6. Number of requests (RPS) per user for internet shop API  | path, method | 
| 7. Number of requests (RPS) per user for CRM API  | path, method  | 
| 8. Number of requests (RPS) per user for MES API  | path, method | 
| 9. CPU % for shop API  | host, service |  
| 10. CPU % for CRM API  | host, service  | 
| 11. CPU % for MES API  | host, service  | 
| 12. Memory Utilisation for shop API  | host, service  | 
| 13. Memory Utilisation for CRM API  | host, service  | 
| 14. Memory Utilisation for MES API  | host, service  |
| 15. Memory Utilisation for shop db instance  |  host, dbName | 
| 16. Memory Utilisation for MES db instance  |  host, dbName | 
| 17. Number of connections for shop db instance  | host, dbName  | 
| 18. Number of connections for MES db instance  | host, dbName  | 
| 19. Response time (latency) for shop API  | path, method  |
| 20. Response time (latency) for CRM API  |  path, method |
| 21. Response time (latency) for MES API  |  path, method |
| 25. Number of `HTTP 200` for shop API  | path, method  | 
| 26. Number of `HTTP 200` for CRM API  |  path, method | 
| 27. Number of `HTTP 200` for MES API  |  path, method |
| 28. Number of `HTTP 500` for shop API  | path, method  |
| 29. Number of `HTTP 500` for CRM API  |  path, method |
| 30. Number of `HTTP 500` for MES API  | path, method  |
| 31. Number of `HTTP 500` for shop API  | path, method  | 
| 32. Number of simultanious sessions for shop API  | path, method  |
| 33. Number of simultanious sessions for CRM API  | path, method  |
| 34. Number of simultanious sessions for MES API  | path, method  | 


### Кроме имеющихся метрик, следует также внедрить следующие:

| Метрика  | Система | Назначение |
|---|---|---|
| DB query duration | Базы данных | Долгое время выполнения запросов покажет, какие структуры данных нуждаются в оптимизиции (добавление кэша, постороение индексов...)  |
| Process state step duration | CRM, MES | Время длительности каждого шага процессов. Позволит собирать SLA-метрики бизнес-процессов |
| Complete process state duration | CRM, MES | Полное время выполняения заказа. Позволит собирать SLA-метрики бизнес-процессов |
| Concumers count | RabbitMQ | Количество активных подписок. Позволит понимать, что нужно горизонтально масштабировать систему |
| Wait time | RabbitMQ  | Время "простоя" сообщений в очереди. Позволит понимать, что системы не справляются с текущей нагрузкой |

## План действий

1. Развернуть kubernetes-кластер
    - пока в нем не будет сервисов, но в будущем, псотепенно, в кластер будут перевезены и сервисы.
2. Развернуть Grafana + Prometeus + ThanosDB + AlertManager в kubernetes.
3. Настроить экспорт метрик из CRM, MES систем. Настроить сбор метрик EC2, Database, RabbitMQ ресурсов.
4. Настроить дашборды в Grafana.
5. Определить лимиты по каждой метрике, согласовать с бизнесом.
6. Настроить уведомления (алерты) по показателям метрик.

## Показатели насыщенности и реакции

1. Метрика: Number of message `in flight` in RabbitMQ 
    1. Порог: 100 сообщений
    2. Реакция: 
        - Нотификация в мессенджеры для SRE
    3. Описание: Если в очереди больше 100 необработанных заказов - значит какая-то система полностью заблокирована, либо находится в нерабочем состоянии
2. Метрика: Process state step duration
    1. Порог: 30 минут
    2. Реакция: 
        - Issue на команду разработки и поддержки MES
    3. Описание: Если шаг процесса длится более 30 минут - зачит, вероятно, ссообщение утеряно, произошел сбой, и процесс требуется восстанавить.
2. Метрика: Complete process state duration
    1. Порог: 60 минут
    2. Реакция:
        - Нотификация в мессенджеры для SRE
        - Issue на команду разработки и поддержки MES
    3. Описание: Если весь процесс не завершился за 60 минут - либо случился сбой конкретного процесса, либо просадка в производительности на каком-то этапе.
4. Метркка: Number of `dead-letter-exchange` letters in RabbitMQ
    1. Порог: > 0
    2. Реакция:
        - Issue на команду разработки и поддержки
    3. Описание: "dead-letter" - это "мертвые" сообщения, которые не смогли быть обработаны автоматически. Все сообщения попавшие в такую очередь требуеют анализа команды разработки, и ручного восстановления процессе по заказу.

