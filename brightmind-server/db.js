const low    = require('lowdb');
const FileSync = require('lowdb/adapters/FileSync');
const path   = require('path');

const file    = path.join(__dirname, 'data', 'db.json');
const adapter = new FileSync(file);
const db      = low(adapter);

// Default schema
db.defaults({ users: [] }).write();

module.exports = db;
