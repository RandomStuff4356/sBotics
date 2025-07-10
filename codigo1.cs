//Motores
Servomotor mDirTras = Bot.GetComponent<Servomotor>("motorDirTras");
Servomotor mDirFrente = Bot.GetComponent<Servomotor>("motorDirFrente");
Servomotor mEsqTras = Bot.GetComponent<Servomotor>("motorEsqTras");
Servomotor mEsqFrente = Bot.GetComponent<Servomotor>("motorEsqFrente");

//Sensores de cor
Color sensorEsquerdo = Bot.GetComponent<ColorSensor>("sensorEsq").Analog;
Color sensorDireito = Bot.GetComponent<ColorSensor>("sensorDir").Analog;
Color sensorCentral = Bot.GetComponent<ColorSensor>("sensorCen").Analog;

//Leituras do R e G dos sensores:
//(0, 1): rg do sensor esquerdo
//(2, 3): rg do sensor central
//(4, 5): rg do sensor direito
//O valor do azul é insignificante, pois os valores de r e g são suficientes para detectar qual estado está senso detectado
double[] leituras = new double[6];

double speedReta = 200; //Target da reta
double speedCurva = 500; //Target da curva 

double corte = 80; //Valor de corte, se maior, é branco, se menor ou igual, é preto

string estado = "indef";

async Task Main()
{
    while (true)
    {
		await lerSensores();

		if (estado == "vermelho") { await parar(); await Time.Delay(7000); }
		else if (estado == "verdeDuplo") { await lopenEsq(speedCurva, 5); }
		else if (estado == "corrigirDir") { await lopenDir(speedCurva, 1); }
		else if (estado == "corrigirEsq") { await lopenEsq(speedCurva, 1); }
		else if (estado == "reta") { await lopenFrente(speedReta, 0.2); }
		await Time.Delay(100);
    }
}

async Task lerSensores()
{
	leituras[0] = sensorEsquerdo.Red;
	leituras[1] = sensorEsquerdo.Green;
	leituras[2] = sensorCentral.Red;
	leituras[3] = sensorCentral.Green;
	leituras[4] = sensorDireito.Red;
	leituras[5] = sensorDireito.Green;

	await definirEstado(10);

	IO.Print($"Sensor esquerdo: {leituras[0]}, {leituras[1]}| Sensor central: {leituras[2]}, {leituras[3]} | Sensor direito: {leituras[4]}, {leituras[5]} | Estado: {estado}");
}

async Task definirEstado(double margemDeErro)
{
	//É implementada uma margem de erro para a checagem
    if(leituras[0] - leituras[1] > margemDeErro || leituras[2] - leituras[3] > margemDeErro || leituras[4] - leituras[5] > margemDeErro)
    {
		estado = "vermelho";//Vermelho do final da pista
    }
	else if(leituras[1] - leituras[0] > margemDeErro && leituras[5] - leituras[4] > margemDeErro)
    {
		estado = "verdeDuplo";
    }
	else if (leituras[1] - leituras[0] > margemDeErro && leituras[5] - leituras[4] < margemDeErro)
    {
		estado = "verdeEsq";//Verde esquerdo
    }
	else if(leituras[5] - leituras[4] > margemDeErro && leituras[1] - leituras[0] < margemDeErro)
    {
		estado = "verdeDir";//Verde direito
    }
	else if (leituras[0] < corte && leituras[4] > corte)
    {
		estado = "corrigirEsq";
    }
	else if (leituras[4] < corte && leituras[0] > corte)
    {
		estado = "corrigirDir";
    }
	else if(leituras[0] - leituras[1] < margemDeErro && leituras[4] - leituras[5] < margemDeErro)
    {
		estado = "reta";//Gap ou linha normal
    }
}

//Target: velocidade que será atingida
//length: duração do movimento em segundos
async Task lopenFrente(double target, double length)
{
	IO.PrintLine("Andando para frente");

	mDirTras.Locked = false;
	mDirFrente.Locked = false;
	mEsqTras.Locked = false;
	mEsqFrente.Locked = false;

	mDirTras.Apply(400, target);
	mDirFrente.Apply(400, target);
	mEsqTras.Apply(400, target);
	mEsqFrente.Apply(400, target);

	await Time.Delay(length * 1000);

	await parar();
}

async Task lopenEsq(double target, double length)
{
	IO.PrintLine("Andando para a esquerda");

	mDirTras.Locked = false;
	mDirFrente.Locked = false;
	mEsqTras.Locked = false;
	mEsqFrente.Locked = false;

	mDirTras.Apply(400, target);
	mDirFrente.Apply(400, target);
	mEsqTras.Apply(400, -target);
	mEsqFrente.Apply(400, -target);

	await Time.Delay(length * 1000);

	await parar();
}

async Task lopenDir(double target, double length)
{
	IO.PrintLine("Andando para a direita");

	mDirTras.Locked = false;
	mDirFrente.Locked = false;
	mEsqTras.Locked = false;
	mEsqFrente.Locked = false;

	mDirTras.Apply(400, -target);
	mDirFrente.Apply(400, -target);
	mEsqTras.Apply(400, target);
	mEsqFrente.Apply(400, target);

	await Time.Delay(length * 1000);

	await parar();
}

async Task parar()
{
	mDirTras.Locked = true;
	mDirFrente.Locked = true;
	mEsqTras.Locked = true;
	mEsqFrente.Locked = true;
}
