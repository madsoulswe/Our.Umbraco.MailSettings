const CopyPlugin = require('copy-webpack-plugin');
const path = require('path');

module.exports = {
    watch: true,
    context: path.resolve(__dirname, './'),
    entry: path.resolve(__dirname, './', 'index.js'),
    output: { path: path.resolve(__dirname, './') },
    plugins: [
        new CopyPlugin({
            patterns: [
                { from: 'Our.Umbraco.MailSettings/wwwroot/App_Plugins/Our.Umbraco.MailSettings/', to: 'Our.Umbraco.MailSettings.WebsiteV10/App_Plugins/Our.Umbraco.MailSettings/' },
            ],
        }),
    ],
};