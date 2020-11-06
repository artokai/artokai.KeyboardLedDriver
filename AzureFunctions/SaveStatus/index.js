
module.exports = async function (context, req) {
    var azure = require('azure-storage');

    context.log('Processing request...');
    if (req.body) {
        context.log.info(JSON.stringify(req.body));
    } else {
        context.log.info("Body is missing!");
    }

    if (!req.body || !req.body.service || !req.body.status) {
        context.log.error("Request is missing service and/or status.");
        context.res = { status: 400, body: "Invalid request! Fields 'service' and 'status' are required." };
        return;
    }

    try {
        var tableService = azure.createTableService();
        await ensureTableExists(context, tableService, "status");
        await insertOrCreate(azure, context, tableService, "status", "pkey_status", req.body.service, req.body.status, (req.body.details || ""));

        context.log.info("Service status updated!");
        context.res = { status: 200, body: "OK" };
        context.done()

    } catch (ex) {
        context.log.error("Error occured: " + ex);
        context.res = { status: 500, body: "Error occured: " + ex }
        context.done()
    }
};

async function insertOrCreate(azure, context, tableService, tableName, partition, serviceName, status, details) {
    context.log.info("Updating or creating service " + serviceName + " status..");
    return new Promise((resolve, reject) => {
        var entGen = azure.TableUtilities.entityGenerator;
        var entity = {
            PartitionKey: entGen.String(partition),
            RowKey: entGen.String(serviceName),
            Service: entGen.String(serviceName),
            UpdateTimeStamp: entGen.DateTime(new Date()),
            Status: entGen.String(status),
            Details: entGen.String(JSON.stringify(details))
        };

        tableService.insertOrReplaceEntity(tableName, entity, {}, (error, result, response) => {
            if (!error) {
                context.log.info("Entity '" + serviceName + "' created or updated succesfully.");
                resolve(result);
            } else {
                context.log.error("An unexpected error occured while upserting entity: " + JSON.stringify(response));
                reject(error);
            }
        });
    });
}

async function ensureTableExists(context, tableService, tableName) {
    context.log("Ensuring table '"+ tableName +"' exists...");
    return new Promise((resolve, reject) => {
        tableService.createTableIfNotExists(tableName, function(error, result, response) {
            if (!error) {;
                context.log.info("Table exists or was created.");
                resolve(result);
            } else {
                context.log.error("An unexpected error occurred while creating table : " + response);
                reject(error);
            }
        });
    });
}
