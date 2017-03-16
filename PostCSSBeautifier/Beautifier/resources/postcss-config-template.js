var scss = require("postcss-scss");

module.exports = {
	parser: scss,
	plugins: [
		require("stylefmt")($$$STYLEFMT$$$),
		require('postcss-sorting')($$$POSTCSSSORTING$$$)
	]
}