var azure = require('azure-storage');

module.exports = async function (context, req) {
    context.log('Processing request...');

    try {
        var tableService = azure.createTableService();
        await ensureTableExists(context, tableService, "status");
        var entries = await fetchStatusEntries(context, tableService, "status", "pkey_status");

        var globalStatus = "ok";
        for (var item of entries) {
            if (item.Status !== "ok") { globalStatus = "error"; }
        }

        var response = {
            "Status" : globalStatus,
            "TimeStamp" : (new Date()).toISOString(),
            "Entries" : entries
        };

        context.res = { status: 200, headers: { 'Content-Type':'application/json' }, body: JSON.stringify(response) };
        context.done()

    } catch (ex) {
        context.log.error("Error occured: " + ex);
        context.res = { status: 500, body: "Error occured: " + ex }
        context.done()
    }
};

async function fetchStatusEntries(context, tableService, tableName, partition) {
    context.log.info("Fetching status entries from " + tableName + "...");
    return new Promise((resolve, reject) => {
        var query = new azure.TableQuery().where('PartitionKey eq ?', partition);
        tableService.queryEntities(tableName, query, null, function(error, result, response) {
            if (!error) {
                context.log.info(JSON.stringify(result.entries));

                var entries = [];
                for (var item of result.entries) {
                    var parsedItem = {};
                    Object.keys(item).forEach(k => {
                        if (k !== ".metadata" && k !== "PartitionKey" && k !== "RowKey") {
                            let prop = Object.getOwnPropertyDescriptor(item, k);
                            if (prop) { parsedItem[k] = prop.value["_"]; }
                        }
                    });

                    try {
                        if (parsedItem.Details) {
                            parsedItem.Details = JSON.parse(parsedItem.Details);
                        }
                    } catch {}

                    entries.push(parsedItem);
                }

                resolve(entries);
            } else {
                context.log.error("An unexpected error occurred while querying entries: " + JSON.stringify(response));
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
