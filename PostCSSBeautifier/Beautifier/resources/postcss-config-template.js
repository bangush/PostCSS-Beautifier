var scss = require("postcss-scss");

module.exports = {
	syntax: scss,
	plugins: [
		require("stylefmt")($$$STYLEFMT$$$),
		require('postcss-sorting')($$$POSTCSSSORTING$$$)
	]
}